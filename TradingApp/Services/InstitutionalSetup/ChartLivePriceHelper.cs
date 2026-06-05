using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Merges a live tick into the most recent chart candle.
/// </summary>
public static class ChartLivePriceHelper
{
    /// <summary>Updates the last candle OHLC with the live price.</summary>
    public static (IReadOnlyList<ChartCandleDto> Candles, decimal LivePrice, DateTimeOffset LiveAt) Apply(
        IReadOnlyList<ChartCandleDto> candles,
        decimal livePrice,
        DateTimeOffset liveAt)
    {
        if (candles.Count == 0)
        {
            return (candles, livePrice, liveAt);
        }

        var last = candles[^1];
        var updated = new ChartCandleDto(
            last.Time,
            last.Open,
            Math.Max(last.High, livePrice),
            Math.Min(last.Low, livePrice),
            livePrice);

        var list = candles.ToList();
        list[^1] = updated;
        return (list, livePrice, liveAt);
    }
}
