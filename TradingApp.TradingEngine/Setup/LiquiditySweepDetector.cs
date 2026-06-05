using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects an inducement sweep around the last corrective swing. For a bullish bias
/// the final low must trade below a prior swing low (taking sell-side liquidity) and
/// price must then close back above that level. The bearish case is mirrored.
/// </summary>
public sealed class LiquiditySweepDetector : ILiquiditySweepDetector
{
    private readonly int _swingStrength;

    /// <summary>
    /// Creates a liquidity sweep detector.
    /// </summary>
    /// <param name="swingStrength">Neighbour count used for fractal swing detection.</param>
    public LiquiditySweepDetector(int swingStrength = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(swingStrength, 1);
        _swingStrength = swingStrength;
    }

    /// <inheritdoc />
    public SweepResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias)
    {
        ArgumentNullException.ThrowIfNull(candles);
        if (bias == MarketBias.Neutral)
        {
            return null;
        }

        return bias == MarketBias.Bullish
            ? DetectBullish(candles)
            : DetectBearish(candles);
    }

    private SweepResult? DetectBullish(IReadOnlyList<Candle> candles)
    {
        var lows = SwingScanner.FindSwingLows(candles, _swingStrength);
        if (lows.Count < 2)
        {
            return null;
        }

        var inducement = lows[^2];
        var sweepLow = lows[^1];
        if (sweepLow.Price >= inducement.Price)
        {
            return null;
        }

        var reclaimed = candles
            .Skip(sweepLow.Index + 1)
            .Any(c => c.Close > inducement.Price);
        if (!reclaimed)
        {
            return null;
        }

        return new SweepResult(
            inducement.Price,
            sweepLow.Price,
            $"Sell-side liquidity at {inducement.Price:F2} swept to {sweepLow.Price:F2} and reclaimed.");
    }

    private SweepResult? DetectBearish(IReadOnlyList<Candle> candles)
    {
        var highs = SwingScanner.FindSwingHighs(candles, _swingStrength);
        if (highs.Count < 2)
        {
            return null;
        }

        var inducement = highs[^2];
        var sweepHigh = highs[^1];
        if (sweepHigh.Price <= inducement.Price)
        {
            return null;
        }

        var reclaimed = candles
            .Skip(sweepHigh.Index + 1)
            .Any(c => c.Close < inducement.Price);
        if (!reclaimed)
        {
            return null;
        }

        return new SweepResult(
            inducement.Price,
            sweepHigh.Price,
            $"Buy-side liquidity at {inducement.Price:F2} swept to {sweepHigh.Price:F2} and reclaimed.");
    }
}
