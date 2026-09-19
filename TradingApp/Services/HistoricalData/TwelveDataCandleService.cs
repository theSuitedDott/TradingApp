using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Loads historical OHLC candles for forex pairs from the Twelve Data REST API.
/// Free tier: 800 credits/day, 8 credits/minute. Supports EUR/USD and GBP/USD.
/// </summary>
public sealed class TwelveDataCandleService(
    IHttpClientFactory httpClientFactory,
    IOptions<TwelveDataSettings> settings,
    ILogger<TwelveDataCandleService> logger)
{
    private readonly TwelveDataSettings _settings = settings.Value;

    /// <summary>Whether the Twelve Data key is present and the service can be used.</summary>
    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>
    /// Fetches OHLC candles from Twelve Data for a forex pair.
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
            logger.LogWarning("TwelveDataCandleService: API key not configured. " +
                "Set via: dotnet user-secrets set \"TwelveData:ApiKey\" \"your-key\"");
            return Array.Empty<Candle>();
        }

        // Twelve Data does not have 4h natively — fetch 1h and aggregate
        var fetchInterval = interval.Equals("4h", StringComparison.OrdinalIgnoreCase) ? "1h" : interval;
        var tdInterval = ToTdInterval(fetchInterval);
        var fetchCount = interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
            ? targetCount * 4
            : targetCount;

        var symbol = ToTdSymbol(oandaSymbol);

        // outputsize capped at 5000 on free tier
        var outputSize = Math.Min(fetchCount, 5000);

        var url = $"time_series?symbol={Uri.EscapeDataString(symbol)}&interval={tdInterval}" +
                  $"&outputsize={outputSize}&dp=5&timezone=UTC&apikey={_settings.ApiKey}";

        logger.LogInformation(
            "Fetching Twelve Data candles: {Symbol} interval={Interval} count={Count}",
            symbol, tdInterval, outputSize);

        var client = httpClientFactory.CreateClient("TwelveData");
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
        if (status != "ok")
        {
            var message = root.TryGetProperty("message", out var m) ? m.GetString() : json[..Math.Min(300, json.Length)];
            logger.LogWarning("Twelve Data returned non-ok status '{Status}': {Message}", status, message);
            return Array.Empty<Candle>();
        }

        if (!root.TryGetProperty("values", out var values))
        {
            logger.LogWarning("Twelve Data response missing 'values' field.");
            return Array.Empty<Candle>();
        }

        var candles = new List<Candle>();
        foreach (var entry in values.EnumerateArray())
        {
            var datetimeStr = entry.TryGetProperty("datetime", out var dt) ? dt.GetString() : null;
            if (datetimeStr == null) continue;

            // Twelve Data returns "YYYY-MM-DD HH:mm:ss" in UTC when timezone=UTC
            if (!DateTimeOffset.TryParse(datetimeStr + " +00:00", out var time)) continue;

            var o = ParseDecimal(entry, "open");
            var h = ParseDecimal(entry, "high");
            var l = ParseDecimal(entry, "low");
            var c = ParseDecimal(entry, "close");

            if (o == null || h == null || l == null || c == null) continue;

            var actualHigh = Math.Max(h.Value, Math.Max(o.Value, c.Value));
            var actualLow  = Math.Min(l.Value, Math.Min(o.Value, c.Value));

            candles.Add(new Candle(time, o.Value, actualHigh, actualLow, c.Value));
        }

        // Twelve Data returns newest-first — reverse to chronological
        candles.Reverse();

        if (candles.Count > 0)
        {
            logger.LogInformation(
                "TwelveData: {Count} candles parsed. First={First:yyyy-MM-dd HH:mm} UTC, Last={Last:yyyy-MM-dd HH:mm} UTC",
                candles.Count,
                candles[0].OpenTime.UtcDateTime,
                candles[^1].OpenTime.UtcDateTime);
        }

        if (interval.Equals("4h", StringComparison.OrdinalIgnoreCase))
        {
            var aggregated = CandleAggregator.Aggregate(candles, TimeSpan.FromHours(4));
            return aggregated.TakeLast(targetCount).ToList();
        }

        return candles.TakeLast(targetCount).ToList();
    }

    private static string ToTdInterval(string interval) =>
        interval.Trim().ToLowerInvariant() switch
        {
            "15m" => "15min",
            "1h"  => "1h",
            "4h"  => "1h",
            _     => "1h"
        };

    private static string ToTdSymbol(string oandaSymbol) =>
        oandaSymbol.Replace('_', '/').ToUpperInvariant();   // EUR_USD → EUR/USD

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
