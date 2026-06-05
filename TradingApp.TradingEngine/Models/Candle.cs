namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Immutable OHLC price bar for a single timeframe interval.
/// </summary>
public sealed class Candle
{
    /// <summary>
    /// Creates an OHLC candle.
    /// </summary>
    /// <param name="openTime">Start time of the bar interval.</param>
    /// <param name="open">Open price.</param>
    /// <param name="high">High price (must be the highest value).</param>
    /// <param name="low">Low price (must be the lowest value).</param>
    /// <param name="close">Close price.</param>
    public Candle(DateTimeOffset openTime, decimal open, decimal high, decimal low, decimal close)
    {
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(open), "OHLC prices must be positive.");
        }

        if (high < low)
        {
            throw new ArgumentException("High must be greater than or equal to low.", nameof(high));
        }

        if (high < Math.Max(open, close) || low > Math.Min(open, close))
        {
            throw new ArgumentException("High/low must envelop open and close.", nameof(high));
        }

        OpenTime = openTime;
        Open = open;
        High = high;
        Low = low;
        Close = close;
    }

    /// <summary>Start time of the bar interval.</summary>
    public DateTimeOffset OpenTime { get; }

    /// <summary>Open price.</summary>
    public decimal Open { get; }

    /// <summary>High price.</summary>
    public decimal High { get; }

    /// <summary>Low price.</summary>
    public decimal Low { get; }

    /// <summary>Close price.</summary>
    public decimal Close { get; }

    /// <summary>Absolute body size (|close - open|).</summary>
    public decimal Body => Math.Abs(Close - Open);

    /// <summary>Full bar range (high - low).</summary>
    public decimal Range => High - Low;

    /// <summary>True when the candle closed above its open.</summary>
    public bool IsBullish => Close > Open;

    /// <summary>True when the candle closed below its open.</summary>
    public bool IsBearish => Close < Open;
}
