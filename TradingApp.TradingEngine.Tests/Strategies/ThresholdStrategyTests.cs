using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Strategies;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Strategies;

public sealed class ThresholdStrategyTests
{
    [Fact]
    public void Evaluate_WhenPriceAboveBuyThreshold_ReturnsBuyWithRiskLevels()
    {
        var sut = new ThresholdStrategy("threshold", buyAbovePrice: 100m, sellBelowPrice: 90m, 2m, 4m);
        var context = new StrategyContext(new MarketSnapshot("SAP", 105m, DateTimeOffset.UtcNow));

        var result = sut.Evaluate(context);

        Assert.Equal(TradingAction.Buy, result.Action);
        Assert.Equal(102.9m, result.StopLossPrice);
        Assert.Equal(109.2m, result.TakeProfitPrice);
    }

    [Fact]
    public void Evaluate_WhenOpenAndPriceBelowSellThreshold_ReturnsSell()
    {
        var sut = new ThresholdStrategy("threshold", buyAbovePrice: 100m, sellBelowPrice: 95m);
        var context = new StrategyContext(
            new MarketSnapshot("SAP", 94m, DateTimeOffset.UtcNow),
            new PositionSnapshot(1m, 100m));

        var result = sut.Evaluate(context);

        Assert.Equal(TradingAction.Sell, result.Action);
    }
}
