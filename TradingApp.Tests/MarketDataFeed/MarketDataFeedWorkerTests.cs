using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingApp.MarketDataFeed;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.MarketDataFeed;

public sealed class MarketDataFeedWorkerTests
{
    [Fact]
    public async Task Worker_WhenAutoStartFalse_DoesNotCallProcessor()
    {
        var processor = new RecordingQuoteProcessor();
        var settings = Options.Create(new MarketDataFeedSettings
        {
            AutoStart = false,
            Symbols = [new SymbolFeedConfig { Symbol = "SAP", Exchange = "XETRA" }]
        });

        var services = new ServiceCollection();
        services.AddScoped<IMarketQuoteProcessor>(_ => processor);
        var provider = services.BuildServiceProvider();

        var feed = new SimulatedMarketDataFeed(NullLogger<SimulatedMarketDataFeed>.Instance);
        var worker = new MarketDataFeedWorker(
            feed,
            provider.GetRequiredService<IServiceScopeFactory>(),
            settings,
            NullLogger<MarketDataFeedWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, processor.ProcessedCount);
    }

    [Fact]
    public async Task Worker_WhenNoSymbols_DoesNotCallProcessor()
    {
        var processor = new RecordingQuoteProcessor();
        var settings = Options.Create(new MarketDataFeedSettings
        {
            AutoStart = true,
            Symbols = []
        });

        var services = new ServiceCollection();
        services.AddScoped<IMarketQuoteProcessor>(_ => processor);
        var provider = services.BuildServiceProvider();

        var feed = new SimulatedMarketDataFeed(NullLogger<SimulatedMarketDataFeed>.Instance);
        var worker = new MarketDataFeedWorker(
            feed,
            provider.GetRequiredService<IServiceScopeFactory>(),
            settings,
            NullLogger<MarketDataFeedWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, processor.ProcessedCount);
    }

    [Fact]
    public async Task Worker_ProcessesQuotesFromFeed()
    {
        var processor = new RecordingQuoteProcessor();
        var settings = Options.Create(new MarketDataFeedSettings
        {
            AutoStart = true,
            IntervalMs = 10,
            Symbols =
            [
                new SymbolFeedConfig
                {
                    Symbol = "SAP",
                    Exchange = "XETRA",
                    SimulatedBasePrice = 100m,
                    SimulatedVolatilityPercent = 0.005m
                }
            ]
        });

        var services = new ServiceCollection();
        services.AddScoped<IMarketQuoteProcessor>(_ => processor);
        var provider = services.BuildServiceProvider();

        var feed = new SimulatedMarketDataFeed(NullLogger<SimulatedMarketDataFeed>.Instance);
        var worker = new MarketDataFeedWorker(
            feed,
            provider.GetRequiredService<IServiceScopeFactory>(),
            settings,
            NullLogger<MarketDataFeedWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(cts.Token);
        await Task.Delay(280);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(processor.ProcessedCount >= 1,
            $"Expected at least 1 processed tick, got {processor.ProcessedCount}.");
    }

    private sealed class RecordingQuoteProcessor : IMarketQuoteProcessor
    {
        private int _count;

        public int ProcessedCount => _count;

        public Task<ServiceResult<MarketQuoteProcessResult>> ProcessQuoteAsync(
            MarketQuoteTick tick,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(
                ServiceResult<MarketQuoteProcessResult>.Success(
                    new MarketQuoteProcessResult(0, 0)));
        }
    }
}
