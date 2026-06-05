using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Models;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Indicators;

public sealed class RsiCalculatorTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Candle Flat(int i, decimal close)
        => new(Origin.AddHours(i), close, close + 0.5m, close - 0.5m, close);

    [Fact]
    public void Calculate_WhenInsufficientData_ReturnsAllNull()
    {
        var candles = Enumerable.Range(0, 10).Select(i => Flat(i, 100m + i)).ToList();

        var rsi = new RsiCalculator().Calculate(candles, period: 14);

        Assert.All(rsi, value => Assert.Null(value));
    }

    [Fact]
    public void Calculate_WhenOnlyGains_ReturnsHundred()
    {
        var candles = Enumerable.Range(0, 20).Select(i => Flat(i, 100m + i)).ToList();

        var rsi = new RsiCalculator().Calculate(candles, period: 14);

        Assert.Equal(100m, rsi[^1]);
    }

    [Fact]
    public void Calculate_ProducesValueWithinBounds()
    {
        decimal[] closes = [100, 102, 101, 103, 102, 104, 103, 105, 104, 106, 105, 107, 106, 108, 107, 109];
        var candles = closes.Select((c, i) => Flat(i, c)).ToList();

        var rsi = new RsiCalculator().Calculate(candles, period: 14);

        Assert.NotNull(rsi[^1]);
        Assert.InRange(rsi[^1]!.Value, 0m, 100m);
    }
}
