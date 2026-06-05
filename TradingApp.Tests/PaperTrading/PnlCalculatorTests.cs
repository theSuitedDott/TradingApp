using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class PnlCalculatorTests
{
    private readonly PnlCalculator _sut = new();

    [Fact]
    public void CalculateUnrealizedLong_ReturnsCorrectProfit()
    {
        var pnl = _sut.CalculateUnrealizedLong(10m, 100m, 110m);
        Assert.Equal(100m, pnl);
    }

    [Fact]
    public void CalculateRealizedLong_ReturnsCorrectProfit()
    {
        var pnl = _sut.CalculateRealizedLong(5m, 100m, 115m);
        Assert.Equal(75m, pnl);
    }
}
