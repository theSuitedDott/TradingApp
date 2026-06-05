using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Tests.Setup;

/// <summary>
/// Helpers to build deterministic candle series for setup detector tests.
/// </summary>
internal static class TestCandles
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a candle from low/high; open and close default to the midpoint.</summary>
    public static Candle Of(int index, decimal low, decimal high, decimal? open = null, decimal? close = null)
    {
        var mid = (low + high) / 2m;
        return new Candle(Origin.AddHours(index), open ?? mid, high, low, close ?? mid);
    }

    /// <summary>Builds a candle from explicit OHLC values.</summary>
    public static Candle Ohlc(int index, decimal open, decimal high, decimal low, decimal close)
        => new(Origin.AddHours(index), open, high, low, close);

    /// <summary>
    /// Bullish structure: rising swing highs and rising swing lows (detected with strength 1).
    /// </summary>
    public static IReadOnlyList<Candle> RisingStructure() =>
    [
        Of(0, 20m, 22m),
        Of(1, 21m, 30m),
        Of(2, 19m, 24m),
        Of(3, 22m, 33m),
        Of(4, 21m, 26m),
        Of(5, 24m, 36m),
        Of(6, 23m, 28m),
        Of(7, 26m, 39m),
        Of(8, 25m, 30m)
    ];

    /// <summary>
    /// Falling structure: declining swing highs and declining swing lows (strength 1).
    /// </summary>
    public static IReadOnlyList<Candle> FallingStructure() =>
    [
        Of(0, 38m, 40m),
        Of(1, 30m, 39m),
        Of(2, 36m, 41m),
        Of(3, 27m, 38m),
        Of(4, 34m, 39m),
        Of(5, 24m, 36m),
        Of(6, 32m, 37m),
        Of(7, 21m, 34m),
        Of(8, 30m, 35m)
    ];
}
