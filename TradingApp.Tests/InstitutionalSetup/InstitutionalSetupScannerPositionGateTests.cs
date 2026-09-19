using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class InstitutionalSetupScannerPositionGateTests
{
    [Fact]
    public async Task ScanAsync_SkipsNotification_WhenOpenPositionExists()
    {
        var store = new TradeOpportunityStore();
        var notifier = new RecordingNotifier();
        var services = new ServiceCollection();
        services.AddSingleton<IOpenPositionLookup>(_ => new StubPositionLookup(hasOpen: true));
        services.AddSingleton<ISetupCandleProvider>(_ => new MockSetupCandleProvider());
        var provider = services.BuildServiceProvider();

        var sut = new InstitutionalSetupScanner(
            new AlwaysSetupStrategy(),
            new MockSetupCandleProvider(),
            store,
            notifier,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new InstitutionalSetupSettings { Symbol = "SAP", Exchange = "XETRA", RsiPeriod = 3 }),
            Options.Create(new OandaSettings { LiveOrderEnabled = false }),
            NullLogger<InstitutionalSetupScanner>.Instance);

        var result = await sut.ScanAsync();

        Assert.Null(result);
        Assert.Equal(0, notifier.NotifyCount);
        Assert.Empty(store.GetAll());
    }

    private sealed class StubPositionLookup(bool hasOpen) : IOpenPositionLookup
    {
        public Task<bool> HasOpenPositionAsync(string symbol, string exchange, CancellationToken cancellationToken = default) =>
            Task.FromResult(hasOpen);
    }

    private sealed class RecordingNotifier : ISetupOpportunityNotifier
    {
        public int NotifyCount { get; private set; }

        public Task NotifyOpportunityAsync(TradeOpportunityDto opportunity, CancellationToken cancellationToken = default)
        {
            NotifyCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysSetupStrategy : IInstitutionalSetupStrategy
    {
        public InstitutionalSetupResult EvaluateAllConditions(InstitutionalSetupInput input) => Evaluate(input);

        public InstitutionalSetupResult Evaluate(InstitutionalSetupInput input) =>
            new(
                input.Symbol,
                input.Exchange,
                MarketBias.Bullish,
                [
                    new ConditionCheck("A", true, "ok"),
                    new ConditionCheck("B", true, "ok"),
                    new ConditionCheck("C", true, "ok"),
                    new ConditionCheck("D", true, "ok"),
                    new ConditionCheck("E", true, "ok"),
                    new ConditionCheck("F", true, "ok")
                ],
                DateTimeOffset.UtcNow,
                entryPrice: 109.75m,
                stopLossPrice: 101.9m,
                takeProfitPrice: 125.45m);
    }
}
