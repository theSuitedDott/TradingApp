using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects a three-candle Fair Value Gap. For a bullish bias the gap exists when the
/// high of the candle before the displacement is below the low of the candle after it.
/// The bearish case is mirrored.
/// </summary>
public sealed class FairValueGapDetector : IFairValueGapDetector
{
    /// <inheritdoc />
    public PriceZone? Detect(IReadOnlyList<Candle> candles, MarketBias bias, int displacementIndex)
    {
        ArgumentNullException.ThrowIfNull(candles);
        if (bias == MarketBias.Neutral)
        {
            return null;
        }

        if (displacementIndex < 1 || displacementIndex >= candles.Count - 1)
        {
            return null;
        }

        var before = candles[displacementIndex - 1];
        var after = candles[displacementIndex + 1];

        if (bias == MarketBias.Bullish)
        {
            return before.High < after.Low
                ? new PriceZone(before.High, after.Low)
                : null;
        }

        return before.Low > after.High
            ? new PriceZone(after.High, before.Low)
            : null;
    }
}
