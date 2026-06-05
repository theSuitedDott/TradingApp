using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// A confirmed swing high or swing low (fractal pivot) in a candle series.
/// </summary>
public sealed class SwingPoint
{
    /// <summary>
    /// Creates a swing point.
    /// </summary>
    /// <param name="index">Index of the pivot candle in the source series.</param>
    /// <param name="candle">The pivot candle.</param>
    /// <param name="isHigh">True for a swing high, false for a swing low.</param>
    public SwingPoint(int index, Candle candle, bool isHigh)
    {
        ArgumentNullException.ThrowIfNull(candle);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Index = index;
        Candle = candle;
        IsHigh = isHigh;
    }

    /// <summary>Index of the pivot candle in the source series.</summary>
    public int Index { get; }

    /// <summary>The pivot candle.</summary>
    public Candle Candle { get; }

    /// <summary>True for a swing high, false for a swing low.</summary>
    public bool IsHigh { get; }

    /// <summary>Pivot price (high for a swing high, low for a swing low).</summary>
    public decimal Price => IsHigh ? Candle.High : Candle.Low;
}
