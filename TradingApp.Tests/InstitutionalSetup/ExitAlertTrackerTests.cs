using TradingApp.Services.InstitutionalSetup;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class ExitAlertTrackerTests
{
    [Fact]
    public void TryRegister_AllowsFirstAlertOnly()
    {
        var sut = new ExitAlertTracker();
        var id = Guid.NewGuid();

        Assert.True(sut.TryRegister(id, "StopLoss"));
        Assert.False(sut.TryRegister(id, "StopLoss"));
        Assert.True(sut.TryRegister(id, "TakeProfit"));
    }

    [Fact]
    public void PruneClosedPositions_ClearsStaleEntries()
    {
        var sut = new ExitAlertTracker();
        var openId = Guid.NewGuid();
        var closedId = Guid.NewGuid();

        sut.TryRegister(openId, "StopLoss");
        sut.TryRegister(closedId, "StopLoss");
        sut.PruneClosedPositions([openId]);

        Assert.False(sut.TryRegister(openId, "StopLoss"));
        Assert.True(sut.TryRegister(closedId, "StopLoss"));
    }
}
