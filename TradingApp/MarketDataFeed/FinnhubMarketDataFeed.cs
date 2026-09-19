using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.PaperTrading;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Polls the Finnhub REST quote endpoint for supported forex pairs and yields mid-price ticks.
/// Finnhub symbol format: OANDA:EUR_USD
/// </summary>
public sealed class FinnhubMarketDataFeed(
    IHttpClientFactory httpClientFactory,
    IOptions<FinnhubSettings> finnhubOptions,
    IOptions<MarketDataFeedSettings> feedOptions,
    ILogger<FinnhubMarketDataFeed> logger) : IMarketDataFeed
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public string ProviderName => "Finnhub";

    /// <inheritdoc />
    public async IAsyncEnumerable<MarketQuoteTick> StreamAsync(
        IReadOnlyList<SymbolFeedConfig> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var settings = finnhubOptions.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning(
                "FinnhubMarketDataFeed: ApiKey not configured under '{Section}' — no ticks will be produced. " +
                "Set via: dotnet user-secrets set \"Finnhub:ApiKey\" \"your-key\"",
                FinnhubSettings.SectionName);
            yield break;
        }

        var instruments = ResolveInstruments(symbols);
        if (instruments.Count == 0)
        {
            logger.LogWarning("FinnhubMarketDataFeed started with no supported forex symbols.");
            yield break;
        }

        logger.LogInformation(
            "FinnhubMarketDataFeed polling {Count} symbol(s): {Symbols}",
            instruments.Count,
            string.Join(", ", instruments.Select(i => i.FinnhubSymbol)));

        var pollDelay = TimeSpan.FromMilliseconds(feedOptions.Value.IntervalMs > 0 ? feedOptions.Value.IntervalMs : 5_000);
        var client = httpClientFactory.CreateClient("Finnhub");

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var instrument in instruments)
            {
                MarketQuoteTick? tick = null;
                try
                {
                    tick = await FetchQuoteAsync(client, instrument, settings.ApiKey, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Finnhub quote poll failed for {Symbol}.", instrument.FinnhubSymbol);
                }

                if (tick is not null)
                {
                    logger.LogDebug(
                        "Finnhub tick {Symbol}.{Exchange} @ {Price:F5}",
                        tick.Symbol, tick.Exchange, tick.Price);
                    yield return tick;
                }
            }

            try
            {
                await Task.Delay(pollDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

    private async Task<MarketQuoteTick?> FetchQuoteAsync(
        HttpClient client,
        InstrumentMapping instrument,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var url = $"quote?symbol={Uri.EscapeDataString(instrument.FinnhubSymbol)}&token={apiKey}";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Finnhub quote failed for {instrument.FinnhubSymbol} ({(int)response.StatusCode}): {body}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var quote = JsonSerializer.Deserialize<FinnhubQuoteResponse>(content, JsonOptions);

        // c == 0 means no data returned (market closed or invalid symbol)
        if (quote is null || quote.Current == 0m)
        {
            return null;
        }

        var timestamp = quote.Timestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(quote.Timestamp)
            : DateTimeOffset.UtcNow;

        return new MarketQuoteTick(instrument.Symbol, instrument.Exchange, quote.Current, timestamp);
    }

    /// <summary>
    /// Maps configured symbols to Finnhub quote symbols (OANDA:EUR_USD format).
    /// Only EUR/USD and GBP/USD are supported.
    /// </summary>
    private static List<InstrumentMapping> ResolveInstruments(IReadOnlyList<SymbolFeedConfig> symbols)
    {
        var result = new List<InstrumentMapping>();
        foreach (var cfg in symbols)
        {
            var oandaInstrument = ForexSymbolNormalizer.ToOandaInstrument(cfg.Symbol);
            if (!ForexSymbolNormalizer.IsOandaForex(oandaInstrument))
            {
                continue;
            }

            var finnhubSymbol = $"OANDA:{oandaInstrument}";
            var exchange = string.IsNullOrWhiteSpace(cfg.Exchange) ? "OANDA" : cfg.Exchange.ToUpperInvariant();
            result.Add(new InstrumentMapping(oandaInstrument, exchange, finnhubSymbol));
        }

        return result;
    }

    private sealed record InstrumentMapping(string Symbol, string Exchange, string FinnhubSymbol);

    private sealed class FinnhubQuoteResponse
    {
        /// <summary>Current price.</summary>
        [JsonPropertyName("c")]
        public decimal Current { get; init; }

        /// <summary>Unix timestamp of the quote.</summary>
        [JsonPropertyName("t")]
        public long Timestamp { get; init; }
    }
}
