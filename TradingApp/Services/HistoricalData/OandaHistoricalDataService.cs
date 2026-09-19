using System.Globalization;
using System.Text.Json;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Fetches OHLC candles from the OANDA v20 REST API for supported forex pairs.
/// </summary>
public sealed class OandaHistoricalDataService(
    IHttpClientFactory httpClientFactory,
    ILogger<OandaHistoricalDataService> logger)
{
    /// <summary>
    /// Returns historical mid-price candles for a supported OANDA instrument.
    /// </summary>
    /// <param name="symbol">OANDA instrument or legacy Yahoo ticker.</param>
    /// <param name="interval">Chart interval (1h, 4h, 1d).</param>
    /// <param name="range">Lookback window (e.g. 60d, 5d).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        string interval,
        string range,
        int? candleCount = null,
        CancellationToken cancellationToken = default) =>
        GetCandlesInternalAsync(symbol, interval, range, cancellationToken);

    private async Task<IReadOnlyList<Candle>> GetCandlesInternalAsync(
        string symbol,
        string interval,
        string range,
        CancellationToken cancellationToken)
    {
        var instrument = ForexSymbolNormalizer.ToOandaInstrument(symbol);
        if (!ForexSymbolNormalizer.IsOandaForex(instrument))
        {
            throw new InvalidOperationException(
                $"Symbol '{symbol}' is not a supported OANDA forex pair. Use EUR_USD or GBP_USD.");
        }

        var granularity = MapGranularity(interval);
        var (from, to) = ParseRange(range);

        var client = httpClientFactory.CreateClient("Oanda");
        var fromParam = Uri.EscapeDataString(from.ToString("O"));
        var toParam = Uri.EscapeDataString(to.ToString("O"));
        var url =
            $"v3/instruments/{instrument}/candles?price=M&granularity={granularity}&from={fromParam}&to={toParam}";

        logger.LogInformation("Fetching OANDA candles for {Instrument} ({Granularity}, {Range}).", instrument, granularity, range);

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"OANDA candle request failed ({(int)response.StatusCode}): {body}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OandaCandlesResponse>(content);
        if (data?.Candles is null || data.Candles.Count == 0)
        {
            return Array.Empty<Candle>();
        }

        var candles = new List<Candle>(data.Candles.Count);
        foreach (var item in data.Candles)
        {
            if (item.Mid is null ||
                !TryParseDecimal(item.Mid.Open, out var open) ||
                !TryParseDecimal(item.Mid.High, out var high) ||
                !TryParseDecimal(item.Mid.Low, out var low) ||
                !TryParseDecimal(item.Mid.Close, out var close) ||
                !DateTimeOffset.TryParse(item.Time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time))
            {
                continue;
            }

            var actualHigh = Math.Max(high, Math.Max(open, close));
            var actualLow = Math.Min(low, Math.Min(open, close));
            candles.Add(new Candle(time, open, actualHigh, actualLow, close));
        }

        return candles;
    }

    private static string MapGranularity(string interval) =>
        interval.Trim().ToLowerInvariant() switch
        {
            "15m" => "M15",
            "1h" => "H1",
            "4h" => "H4",
            "1d" => "D",
            _ => throw new ArgumentException($"Unsupported OANDA interval '{interval}'. Use 15m, 1h, 4h or 1d.", nameof(interval))
        };

    private static (DateTimeOffset From, DateTimeOffset To) ParseRange(string range)
    {
        var normalized = range.Trim().ToLowerInvariant();
        if (normalized.Length < 2 || !char.IsDigit(normalized[0]))
        {
            throw new ArgumentException($"Unsupported range '{range}'. Use e.g. 60d or 5d.", nameof(range));
        }

        var value = int.Parse(normalized[..^1], CultureInfo.InvariantCulture);
        var unit = normalized[^1];
        var to = DateTimeOffset.UtcNow;

        var from = unit switch
        {
            'd' => to.AddDays(-value),
            'h' => to.AddHours(-value),
            'w' => to.AddDays(-value * 7),
            _ => throw new ArgumentException($"Unsupported range unit in '{range}'.", nameof(range))
        };

        return (from, to);
    }

    private static bool TryParseDecimal(string? value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}
