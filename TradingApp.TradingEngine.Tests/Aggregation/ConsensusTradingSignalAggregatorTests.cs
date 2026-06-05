using TradingApp.TradingEngine.Aggregation;
using TradingApp.TradingEngine.Models;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Aggregation;

public sealed class ConsensusTradingSignalAggregatorTests
{
    private readonly ConsensusTradingSignalAggregator _sut = new();

    [Fact]
    public void Aggregate_WhenMajorityBuyAndNoPosition_ReturnsBuy()
    {
        var evaluations = new[]
        {
            new StrategyEvaluation("a", TradingAction.Buy, 90m, 110m),
            new StrategyEvaluation("b", TradingAction.Buy, 91m, 111m),
            new StrategyEvaluation("c", TradingAction.None)
        };

        var result = _sut.Aggregate(evaluations, hasOpenPosition: false);

        Assert.NotNull(result);
        Assert.Equal(TradingAction.Buy, result.Action);
        Assert.Equal("consensus", result.StrategyId);
    }

    [Fact]
    public void Aggregate_WhenOnlyMinorityBuy_ReturnsNull()
    {
        var evaluations = new[]
        {
            new StrategyEvaluation("a", TradingAction.Buy, 90m, 110m),
            new StrategyEvaluation("b", TradingAction.None),
            new StrategyEvaluation("c", TradingAction.None)
        };

        Assert.Null(_sut.Aggregate(evaluations, hasOpenPosition: false));
    }

    [Fact]
    public void Aggregate_WhenMajoritySellWithPosition_ReturnsSell()
    {
        var evaluations = new[]
        {
            new StrategyEvaluation("a", TradingAction.Sell),
            new StrategyEvaluation("b", TradingAction.Sell),
            new StrategyEvaluation("c", TradingAction.None)
        };

        var result = _sut.Aggregate(evaluations, hasOpenPosition: true);

        Assert.NotNull(result);
        Assert.Equal(TradingAction.Sell, result.Action);
    }
}
