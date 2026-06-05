using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Structure-based trend filter: a clear trend requires the two most recent
/// swing highs and swing lows to be consistently rising (bullish) or falling (bearish).
/// </summary>
public sealed class SwingTrendFilter : ITrendFilter
{
    private readonly int _swingStrength;

    /// <summary>
    /// Creates a swing-structure trend filter.
    /// </summary>
    /// <param name="swingStrength">Neighbour count used for fractal swing detection.</param>
    public SwingTrendFilter(int swingStrength = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(swingStrength, 1);
        _swingStrength = swingStrength;
    }

    /// <inheritdoc />
    public MarketBias DetermineBias(IReadOnlyList<Candle> candles)
    {
        ArgumentNullException.ThrowIfNull(candles);

        var highs = SwingScanner.FindSwingHighs(candles, _swingStrength);
        var lows = SwingScanner.FindSwingLows(candles, _swingStrength);
        if (highs.Count < 2 || lows.Count < 2)
        {
            return MarketBias.Neutral;
        }

        var lastHigh = highs[^1].Price;
        var prevHigh = highs[^2].Price;
        var lastLow = lows[^1].Price;
        var prevLow = lows[^2].Price;

        if (lastHigh > prevHigh && lastLow > prevLow)
        {
            return MarketBias.Bullish;
        }

        if (lastHigh < prevHigh && lastLow < prevLow)
        {
            return MarketBias.Bearish;
        }

        return MarketBias.Neutral;
    }
}
