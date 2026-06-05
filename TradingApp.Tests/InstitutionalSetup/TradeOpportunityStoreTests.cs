using TradingApp.DTOs.Setup;
using TradingApp.Services.InstitutionalSetup;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class TradeOpportunityStoreTests
{
    private static TradeOpportunityDto Opportunity(string symbol = "SAP", string direction = "Long") =>
        new(
            Guid.NewGuid(), symbol, "XETRA", direction, direction == "Long" ? "Buy" : "Sell",
            100m, 95m, 110m, 2m, 1m, "msg", DateTimeOffset.UtcNow,
            TradeOpportunityStore.ActiveStatus, []);

    [Fact]
    public void Add_BeyondCapacity_EvictsOldest()
    {
        var store = new TradeOpportunityStore(capacity: 2);
        var first = Opportunity();
        store.Add(first);
        store.Add(Opportunity());
        store.Add(Opportunity());

        Assert.Equal(2, store.GetAll().Count);
        Assert.Null(store.Find(first.Id));
    }

    [Fact]
    public void MarkExecuted_UpdatesStatus()
    {
        var store = new TradeOpportunityStore();
        var opp = Opportunity();
        store.Add(opp);

        var updated = store.MarkExecuted(opp.Id);

        Assert.NotNull(updated);
        Assert.Equal(TradeOpportunityStore.ExecutedStatus, updated!.Status);
        Assert.False(store.HasActive("SAP", "Long"));
    }

    [Fact]
    public void HasActive_MatchesSymbolAndDirection()
    {
        var store = new TradeOpportunityStore();
        store.Add(Opportunity("SAP", "Long"));

        Assert.True(store.HasActive("sap", "long"));
        Assert.False(store.HasActive("SAP", "Short"));
        Assert.False(store.HasActive("AAPL", "Long"));
    }
}
