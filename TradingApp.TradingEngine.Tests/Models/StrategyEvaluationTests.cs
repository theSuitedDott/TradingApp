using TradingApp.TradingEngine.Models;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Models;

public sealed class StrategyEvaluationTests
{
    [Fact]
    public void Constructor_WhenStopLossAboveTakeProfitForBuy_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StrategyEvaluation("s", TradingAction.Buy, stopLossPrice: 110m, takeProfitPrice: 100m));
    }
}
