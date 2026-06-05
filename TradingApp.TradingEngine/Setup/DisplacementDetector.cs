using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Flags a candle as displacement when its body exceeds a multiple of the recent
/// average body and it closes in the trend direction.
/// </summary>
public sealed class DisplacementDetector : IDisplacementDetector
{
    private readonly decimal _bodyMultiplier;
    private readonly int _lookback;

    /// <summary>
    /// Creates a displacement detector.
    /// </summary>
    /// <param name="bodyMultiplier">Required body size relative to the recent average body.</param>
    /// <param name="lookback">Number of preceding candles used for the average body.</param>
    public DisplacementDetector(decimal bodyMultiplier = 1.5m, int lookback = 10)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bodyMultiplier, 1m);
        ArgumentOutOfRangeException.ThrowIfLessThan(lookback, 2);

        _bodyMultiplier = bodyMultiplier;
        _lookback = lookback;
    }

    /// <inheritdoc />
    public DisplacementResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias)
    {
        ArgumentNullException.ThrowIfNull(candles);
        if (bias == MarketBias.Neutral)
        {
            return null;
        }

        for (var i = candles.Count - 1; i >= 1; i--)
        {
            var candle = candles[i];
            var alignsWithTrend = bias == MarketBias.Bullish ? candle.IsBullish : candle.IsBearish;
            if (!alignsWithTrend)
            {
                continue;
            }

            var averageBody = AverageBodyBefore(candles, i);
            if (averageBody <= 0)
            {
                continue;
            }

            var ratio = candle.Body / averageBody;
            if (ratio >= _bodyMultiplier)
            {
                return new DisplacementResult(
                    i,
                    candle,
                    ratio,
                    $"Displacement candle body {ratio:F2}x the recent average in trend direction.");
            }
        }

        return null;
    }

    private decimal AverageBodyBefore(IReadOnlyList<Candle> candles, int index)
    {
        var start = Math.Max(0, index - _lookback);
        var count = index - start;
        if (count == 0)
        {
            return 0m;
        }

        decimal sum = 0m;
        for (var i = start; i < index; i++)
        {
            sum += candles[i].Body;
        }

        return sum / count;
    }
}
