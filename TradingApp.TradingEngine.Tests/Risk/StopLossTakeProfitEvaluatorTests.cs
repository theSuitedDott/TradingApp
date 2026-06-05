using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Risk;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Risk;

public sealed class StopLossTakeProfitEvaluatorTests
{
    private readonly StopLossTakeProfitEvaluator _sut = new();

    [Fact]
    public void Evaluate_WhenPriceHitsStopLoss_ReturnsSellWithStopLossReason()
    {
        var context = new PositionRiskContext(
            new MarketSnapshot("SAP", 95m, DateTimeOffset.UtcNow),
            new PositionSnapshot(10m, 100m, stopLossPrice: 96m, takeProfitPrice: 120m));

        var result = _sut.Evaluate(context);

        Assert.NotNull(result);
        Assert.Equal(TradingAction.Sell, result.Action);
        Assert.Equal(ExitReason.StopLoss, result.Reason);
        Assert.Equal(95m, result.TriggerPrice);
    }

    [Fact]
    public void Evaluate_WhenPriceHitsTakeProfit_ReturnsSellWithTakeProfitReason()
    {
        var context = new PositionRiskContext(
            new MarketSnapshot("SAP", 125m, DateTimeOffset.UtcNow),
            new PositionSnapshot(10m, 100m, stopLossPrice: 90m, takeProfitPrice: 120m));

        var result = _sut.Evaluate(context);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.TakeProfit, result.Reason);
    }

    [Fact]
    public void Evaluate_WhenPriceWithinBand_ReturnsNull()
    {
        var context = new PositionRiskContext(
            new MarketSnapshot("SAP", 105m, DateTimeOffset.UtcNow),
            new PositionSnapshot(10m, 100m, stopLossPrice: 90m, takeProfitPrice: 120m));

        Assert.Null(_sut.Evaluate(context));
    }
}
