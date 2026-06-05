using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class PositionRiskMonitorTests
{
    private readonly PositionRiskMonitor _sut = new();

    [Fact]
    public void GetTriggeredExits_WhenStopLossHit_ReturnsExit()
    {
        var position = new Position
        {
            Id = Guid.NewGuid(),
            PortfolioId = Guid.NewGuid(),
            Symbol = "SAP",
            Exchange = "XETRA",
            Side = OrderSide.Buy,
            Quantity = 5,
            AverageEntryPrice = 100,
            StopLossPrice = 95,
            Status = PositionStatus.Open,
            OpenedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var exits = _sut.GetTriggeredExits(position, Guid.NewGuid(), 94m);

        Assert.Single(exits);
        Assert.Equal(RiskExitReason.StopLoss, exits[0].Reason);
    }
}
