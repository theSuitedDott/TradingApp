using TradingApp.TradingEngine.Aggregation;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Risk;
using TradingApp.TradingEngine.Strategies;
using Xunit;

namespace TradingApp.TradingEngine.Tests;

public sealed class TradingEngineServiceTests
{
    [Fact]
    public void Evaluate_WhenStopLossHit_SkipsAggregationAndReturnsRiskExit()
    {
        var engine = new TradingEngineService(
            new StopLossTakeProfitEvaluator(),
            new ConsensusTradingSignalAggregator());

        var strategies = new ITradingStrategy[]
        {
            new ThresholdStrategy("t1", buyAbovePrice: 50m, sellBelowPrice: 40m)
        };

        var request = new TradingEngineRequest(
            new MarketSnapshot("SAP", 89m, DateTimeOffset.UtcNow),
            strategies,
            new PositionSnapshot(5m, 100m, stopLossPrice: 90m, takeProfitPrice: 130m));

        var result = engine.Evaluate(request);

        Assert.NotNull(result.RiskExit);
        Assert.Equal(ExitReason.StopLoss, result.RiskExit.Reason);
        Assert.Null(result.AggregatedSignal);
        Assert.Equal(TradingAction.Sell, result.EffectiveAction);
        Assert.Single(result.StrategyEvaluations);
    }

    [Fact]
    public void Evaluate_WithMultipleStrategies_ReturnsAllEvaluations()
    {
        var engine = new TradingEngineService(
            new StopLossTakeProfitEvaluator(),
            new ConsensusTradingSignalAggregator());

        var strategies = new ITradingStrategy[]
        {
            new ThresholdStrategy("threshold", buyAbovePrice: 100m, sellBelowPrice: 80m),
            new ThresholdStrategy("threshold-2", buyAbovePrice: 105m, sellBelowPrice: 85m)
        };

        var request = new TradingEngineRequest(
            new MarketSnapshot("SAP", 110m, DateTimeOffset.UtcNow),
            strategies);

        var result = engine.Evaluate(request);

        Assert.Equal(2, result.StrategyEvaluations.Count);
        Assert.All(result.StrategyEvaluations, e => Assert.Equal(TradingAction.Buy, e.Action));
        Assert.NotNull(result.AggregatedSignal);
        Assert.Equal(TradingAction.Buy, result.EffectiveAction);
    }
}
