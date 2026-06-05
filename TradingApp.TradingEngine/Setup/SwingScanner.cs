using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects fractal swing highs and lows in an ordered candle series.
/// A pivot is confirmed when its extreme is the most extreme value within
/// <c>strength</c> neighbours on each side.
/// </summary>
public static class SwingScanner
{
    /// <summary>
    /// Returns all swing highs and lows in chronological order.
    /// </summary>
    /// <param name="candles">Chronologically ordered candles (oldest first).</param>
    /// <param name="strength">Number of neighbouring candles required on each side.</param>
    /// <returns>Swing points ordered by index.</returns>
    public static IReadOnlyList<SwingPoint> FindSwings(IReadOnlyList<Candle> candles, int strength = 2)
    {
        ArgumentNullException.ThrowIfNull(candles);
        ArgumentOutOfRangeException.ThrowIfLessThan(strength, 1);

        var swings = new List<SwingPoint>();
        for (var i = strength; i < candles.Count - strength; i++)
        {
            if (IsSwingHigh(candles, i, strength))
            {
                swings.Add(new SwingPoint(i, candles[i], isHigh: true));
            }
            else if (IsSwingLow(candles, i, strength))
            {
                swings.Add(new SwingPoint(i, candles[i], isHigh: false));
            }
        }

        return swings;
    }

    /// <summary>Returns only swing highs in chronological order.</summary>
    /// <param name="candles">Chronologically ordered candles.</param>
    /// <param name="strength">Neighbour count on each side.</param>
    public static IReadOnlyList<SwingPoint> FindSwingHighs(IReadOnlyList<Candle> candles, int strength = 2)
        => FindSwings(candles, strength).Where(s => s.IsHigh).ToList();

    /// <summary>Returns only swing lows in chronological order.</summary>
    /// <param name="candles">Chronologically ordered candles.</param>
    /// <param name="strength">Neighbour count on each side.</param>
    public static IReadOnlyList<SwingPoint> FindSwingLows(IReadOnlyList<Candle> candles, int strength = 2)
        => FindSwings(candles, strength).Where(s => !s.IsHigh).ToList();

    private static bool IsSwingHigh(IReadOnlyList<Candle> candles, int index, int strength)
    {
        var pivot = candles[index].High;
        for (var offset = 1; offset <= strength; offset++)
        {
            if (candles[index - offset].High >= pivot || candles[index + offset].High >= pivot)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSwingLow(IReadOnlyList<Candle> candles, int index, int strength)
    {
        var pivot = candles[index].Low;
        for (var offset = 1; offset <= strength; offset++)
        {
            if (candles[index - offset].Low <= pivot || candles[index + offset].Low <= pivot)
            {
                return false;
            }
        }

        return true;
    }
}
