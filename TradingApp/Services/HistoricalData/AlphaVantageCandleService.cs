using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Loads historical OHLC candles for forex pairs from the Alpha Vantage REST API.
/// Supports EUR/USD and GBP/USD. Free tier: 25 requests/day, 5/minute.
/// </summary>
public sealed class AlphaVantageCandleService(
    IHttpClientFactory httpClientFactory,
    IOptions<AlphaVantageSettings> settings,
    ILogger<AlphaVantageCandleService> logger)
{
    private readonly AlphaVantageSettings _settings = settings.Value;

    /// <summary>Whether the Alpha Vantage key is present and the service can be used.</summary>
    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>
    /// Fetches OHLC candles from Alpha Vantage for a forex pair.
    /// </summary>
    /// <param name="oandaSymbol">OANDA instrument id, e.g. <c>EUR_USD</c>.</param>
    /// <param name="interval">Candle interval: 15m, 1h, 4h.</param>
    /// <param name="targetCount">Maximum number of candles to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string oandaSymbol,
        string interval,
        int targetCount,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            logger.LogWarning("AlphaVantageCandleService: API key not configured. " +
                "Set via: dotnet user-secrets set \"AlphaVantage:ApiKey\" \"your-key\"");
            return Array.Empty<Candle>();
        }

        // Alpha Vantage does not have 4h natively — fetch 60min and aggregate
        var fetchInterval = interval.Equals("4h", StringComparison.OrdinalIgnoreCase) ? "1h" : interval;
        var avInterval = ToAvInterval(fetchInterval);
        var (fromSymbol, toSymbol) = SplitPair(oandaSymbol);

        // "full" returns up to 30 days of intraday data; "compact" is last 100 points
        var outputSize = targetCount > 100 ? "full" : "compact";

        var url = $"query?function=FX_INTRADAY&from_symbol={fromSymbol}&to_symbol={toSymbol}" +
                  $"&interval={avInterval}&outputsize={outputSize}&apikey={_settings.ApiKey}";

        logger.LogInformation(
            "Fetching Alpha Vantage candles: {FromSymbol}/{ToSymbol} interval={Interval} outputsize={OutputSize}",
            fromSymbol, toSymbol, avInterval, outputSize);

        var client = httpClientFactory.CreateClient("AlphaVantage");
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Alpha Vantage returns an "Information" field when rate-limited or API key invalid
        if (root.TryGetProperty("Information", out var info) ||
            root.TryGetProperty("Note", out info))
        {
            logger.LogWarning("Alpha Vantage API message: {Message}", info.GetString());
            return Array.Empty<Candle>();
        }

        var seriesKey = $"Time Series FX ({avInterval})";
        if (!root.TryGetProperty(seriesKey, out var series))
        {
            logger.LogWarning("Alpha Vantage response missing key '{Key}'. Raw: {Json}",
                seriesKey, json[..Math.Min(500, json.Length)]);
            return Array.Empty<Candle>();
        }

        var candles = new List<Candle>();
        foreach (var entry in series.EnumerateObject())
        {
            if (!DateTimeOffset.TryParse(entry.Name + " +00:00", out var time)) continue;

            var o = ParseDecimal(entry.Value, "1. open");
            var h = ParseDecimal(entry.Value, "2. high");
            var l = ParseDecimal(entry.Value, "3. low");
            var c = ParseDecimal(entry.Value, "4. close");

            if (o == null || h == null || l == null || c == null) continue;

            var actualHigh = Math.Max(h.Value, Math.Max(o.Value, c.Value));
            var actualLow  = Math.Min(l.Value, Math.Min(o.Value, c.Value));

            candles.Add(new Candle(time, o.Value, actualHigh, actualLow, c.Value));
        }

        // Alpha Vantage returns newest-first — reverse to chronological order
        candles.Reverse();

        if (interval.Equals("4h", StringComparison.OrdinalIgnoreCase))
        {
            var aggregated = CandleAggregator.Aggregate(candles, TimeSpan.FromHours(4));
            return aggregated.TakeLast(targetCount).ToList();
        }

        return candles.TakeLast(targetCount).ToList();
    }

    private static string ToAvInterval(string interval) =>
        interval.Trim().ToLowerInvariant() switch
        {
            "15m" => "15min",
            "1h"  => "60min",
            "4h"  => "60min",
            _     => "60min"
        };

    private static (string From, string To) SplitPair(string oandaSymbol)
    {
        var parts = oandaSymbol.Replace('/', '_').ToUpperInvariant().Split('_');
        return parts.Length == 2 ? (parts[0], parts[1]) : ("EUR", "USD");
    }

    private static decimal? ParseDecimal(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) &&
            decimal.TryParse(prop.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        return null;
    }
}
