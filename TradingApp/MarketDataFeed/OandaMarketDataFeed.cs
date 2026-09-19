using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.PaperTrading;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Polls OANDA pricing for supported forex pairs and yields mid-price ticks.
/// </summary>
public sealed class OandaMarketDataFeed(
    IHttpClientFactory httpClientFactory,
    IOptions<OandaSettings> oandaOptions,
    IOptions<MarketDataFeedSettings> feedOptions,
    ILogger<OandaMarketDataFeed> logger) : IMarketDataFeed
{
    private readonly OandaSettings _oanda = oandaOptions.Value;
    private readonly MarketDataFeedSettings _feed = feedOptions.Value;

    /// <inheritdoc />
    public string ProviderName => "OANDA";

    /// <inheritdoc />
    public async IAsyncEnumerable<MarketQuoteTick> StreamAsync(
        IReadOnlyList<SymbolFeedConfig> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_oanda.IsConfigured)
        {
            logger.LogWarning(
                "OandaMarketDataFeed: credentials missing under '{Section}' — no ticks will be produced.",
                OandaSettings.SectionName);
            yield break;
        }

        var instruments = ResolveInstruments(symbols);
        if (instruments.Count == 0)
        {
            logger.LogWarning("OandaMarketDataFeed started with no supported forex symbols.");
            yield break;
        }

        logger.LogInformation(
            "OandaMarketDataFeed polling {Count} instrument(s): {Instruments}",
            instruments.Count,
            string.Join(", ", instruments.Select(i => i.Instrument)));

        var pollDelay = TimeSpan.FromMilliseconds(_feed.IntervalMs > 0 ? _feed.IntervalMs : 5_000);
        var client = httpClientFactory.CreateClient("Oanda");

        while (!cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<MarketQuoteTick> ticks;
            try
            {
                ticks = await FetchPricingAsync(client, instruments, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OANDA pricing poll failed; retrying in {Delay} ms.", pollDelay.TotalMilliseconds);
                await Task.Delay(pollDelay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            foreach (var tick in ticks)
            {
                logger.LogDebug("OANDA tick {Symbol}.{Exchange} @ {Price:F5}", tick.Symbol, tick.Exchange, tick.Price);
                yield return tick;
            }

            await Task.Delay(pollDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<MarketQuoteTick>> FetchPricingAsync(
        HttpClient client,
        IReadOnlyList<InstrumentMapping> instruments,
        CancellationToken cancellationToken)
    {
        var instrumentList = string.Join(",", instruments.Select(i => i.Instrument));
        var url = $"v3/accounts/{_oanda.AccountId}/pricing?instruments={instrumentList}";

        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"OANDA pricing failed ({(int)response.StatusCode}): {body}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<OandaPricingResponse>(content);
        if (data?.Prices is null || data.Prices.Count == 0)
        {
            return Array.Empty<MarketQuoteTick>();
        }

        var ticks = new List<MarketQuoteTick>(data.Prices.Count);
        foreach (var price in data.Prices)
        {
            if (price.Instrument is null)
            {
                continue;
            }

            var mapping = instruments.FirstOrDefault(i =>
                string.Equals(i.Instrument, price.Instrument, StringComparison.OrdinalIgnoreCase));
            if (mapping is null)
            {
                continue;
            }

            var bid = ParseFirstPrice(price.Bids);
            var ask = ParseFirstPrice(price.Asks);
            if (bid is null || ask is null)
            {
                continue;
            }

            var mid = Math.Round((bid.Value + ask.Value) / 2m, 5);
            var timestamp = DateTimeOffset.TryParse(
                price.Time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedTime)
                ? parsedTime
                : DateTimeOffset.UtcNow;

            ticks.Add(new MarketQuoteTick(mapping.Symbol, mapping.Exchange, mid, timestamp));
        }

        return ticks;
    }

    private static decimal? ParseFirstPrice(IReadOnlyList<OandaQuoteLevel>? levels)
    {
        var raw = levels?.FirstOrDefault()?.Price;
        return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static List<InstrumentMapping> ResolveInstruments(IReadOnlyList<SymbolFeedConfig> symbols)
    {
        var result = new List<InstrumentMapping>();
        foreach (var cfg in symbols)
        {
            var instrument = ForexSymbolNormalizer.ToOandaInstrument(cfg.Symbol);
            if (!ForexSymbolNormalizer.IsOandaForex(instrument))
            {
                continue;
            }

            result.Add(new InstrumentMapping(
                instrument,
                instrument,
                string.IsNullOrWhiteSpace(cfg.Exchange) ? "OANDA" : cfg.Exchange.ToUpperInvariant()));
        }

        return result;
    }

    private sealed record InstrumentMapping(string Instrument, string Symbol, string Exchange);
}
