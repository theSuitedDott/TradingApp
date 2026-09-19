using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Loads historical OHLC candles for forex pairs from the Finnhub REST API.
/// Supports EUR/USD and GBP/USD via the <c>OANDA:EUR_USD</c> symbol format.
/// </summary>
public sealed class FinnhubCandleService(
    IHttpClientFactory httpClientFactory,
    IOptions<FinnhubSettings> settings,
    ILogger<FinnhubCandleService> logger)
{
    private readonly FinnhubSettings _settings = settings.Value;

    /// <summary>
    /// Fetches OHLC candles from Finnhub for an OANDA forex symbol.
    /// </summary>
    /// <param name="oandaSymbol">OANDA instrument id, e.g. <c>EUR_USD</c>.</param>
    /// <param name="interval">Candle interval: 15m, 1h, 4h, 1d.</param>
    /// <param name="candleCount">Maximum number of candles to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string oandaSymbol,
        string interval,
        int candleCount,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            logger.LogWarning("FinnhubCandleService: API key not configured.");
            return Array.Empty<Candle>();
        }

        var resolution = ToFinnhubResolution(interval);
        var fetchInterval = interval.Equals("4h", StringComparison.OrdinalIgnoreCase) ? "1h" : interval;
        var fetchCount = interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
            ? candleCount * 4
            : candleCount;
        var actualResolution = ToFinnhubResolution(fetchInterval);

        var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var from = to - (long)fetchCount * IntervalToSeconds(fetchInterval);

        var finnhubSymbol = $"OANDA:{oandaSymbol.Replace('/', '_').ToUpperInvariant()}";
        var url = $"forex/candle?symbol={Uri.EscapeDataString(finnhubSymbol)}&resolution={actualResolution}&from={from}&to={to}&token={_settings.ApiKey}";

        logger.LogInformation("Fetching Finnhub candles: {Symbol} res={Resolution} from={From} to={To}",
            finnhubSymbol, actualResolution, from, to);

        var client = httpClientFactory.CreateClient("Finnhub");
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<FinnhubCandleResponse>(json);

        if (data?.Status != "ok" || data.Timestamps == null || data.Close == null)
        {
            logger.LogWarning("Finnhub returned no candle data for {Symbol}. Status: {Status}",
                finnhubSymbol, data?.Status ?? "null");
            return Array.Empty<Candle>();
        }

        var candles = new List<Candle>(data.Timestamps.Length);
        for (int i = 0; i < data.Timestamps.Length; i++)
        {
            var o = data.Open?[i];
            var h = data.High?[i];
            var l = data.Low?[i];
            var c = data.Close[i];

            if (o == null || h == null || l == null) continue;

            var time = DateTimeOffset.FromUnixTimeSeconds(data.Timestamps[i]);
            var actualHigh = Math.Max(h.Value, Math.Max(o.Value, c));
            var actualLow = Math.Min(l.Value, Math.Min(o.Value, c));

            candles.Add(new Candle(time, (decimal)o.Value, (decimal)actualHigh, (decimal)actualLow, (decimal)c));
        }

        if (interval.Equals("4h", StringComparison.OrdinalIgnoreCase))
        {
            var aggregated = CandleAggregator.Aggregate(candles, TimeSpan.FromHours(4));
            return aggregated.TakeLast(candleCount).ToList();
        }

        return candles.TakeLast(candleCount).ToList();
    }

    private static string ToFinnhubResolution(string interval) =>
        interval.Trim().ToLowerInvariant() switch
        {
            "15m" => "15",
            "1h"  => "60",
            "4h"  => "240",
            "1d"  => "D",
            _     => "60"
        };

    private static int IntervalToSeconds(string interval) =>
        interval.Trim().ToLowerInvariant() switch
        {
            "15m" => 900,
            "1h"  => 3_600,
            "4h"  => 14_400,
            "1d"  => 86_400,
            _     => 3_600
        };
}

/// <summary>Finnhub <c>GET /forex/candle</c> response payload.</summary>
file sealed class FinnhubCandleResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("s")]
    public string? Status { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public long[]? Timestamps { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("o")]
    public double[]? Open { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("h")]
    public double[]? High { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("l")]
    public double[]? Low { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("c")]
    public double[]? Close { get; init; }
}
