using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Strategies;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Strategies;

public sealed class MovingAverageCrossoverStrategyTests
{
    [Fact]
    public void Evaluate_WhenInsufficientHistory_ReturnsNone()
    {
        var sut = new MovingAverageCrossoverStrategy("ma", fastPeriod: 2, slowPeriod: 5);
        var context = new StrategyContext(
            new MarketSnapshot("SAP", 100m, DateTimeOffset.UtcNow, priceHistory: [100m, 101m, 102m]));

        var result = sut.Evaluate(context);

        Assert.Equal(TradingAction.None, result.Action);
        Assert.Contains("Insufficient", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenFastAboveSlow_ReturnsBuy()
    {
        var sut = new MovingAverageCrossoverStrategy("ma", fastPeriod: 2, slowPeriod: 3);
        var history = new[] { 98m, 99m, 100m, 102m, 105m };
        var context = new StrategyContext(
            new MarketSnapshot("SAP", 105m, DateTimeOffset.UtcNow, priceHistory: history));

        var result = sut.Evaluate(context);

        Assert.Equal(TradingAction.Buy, result.Action);
        Assert.NotNull(result.StopLossPrice);
        Assert.NotNull(result.TakeProfitPrice);
    }
}
