using Microsoft.Extensions.Logging.Abstractions;
using TradingApp.MarketDataFeed;
using Xunit;

namespace TradingApp.Tests.MarketDataFeed;

public sealed class SimulatedMarketDataFeedTests
{
    private readonly SimulatedMarketDataFeed _sut =
        new(NullLogger<SimulatedMarketDataFeed>.Instance);

    [Fact]
    public void ProviderName_IsSimulated()
    {
        Assert.Equal("Simulated", _sut.ProviderName);
    }

    [Fact]
    public async Task StreamAsync_NoSymbols_YieldsNoTicks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var ticks = new List<object>();

        await foreach (var tick in _sut.StreamAsync([], cts.Token))
        {
            ticks.Add(tick);
        }

        Assert.Empty(ticks);
    }

    [Fact]
    public async Task StreamAsync_ProducesTicksForEachSymbol()
    {
        using var cts = new CancellationTokenSource();

        var symbols = new List<SymbolFeedConfig>
        {
            new() { Symbol = "SAP", Exchange = "XETRA", SimulatedBasePrice = 180m, SimulatedVolatilityPercent = 0.01m },
            new() { Symbol = "BMW", Exchange = "XETRA", SimulatedBasePrice = 85m,  SimulatedVolatilityPercent = 0.01m }
        };

        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in _sut.StreamAsync(symbols, cts.Token))
        {
            ticks.Add(tick);
            if (ticks.Count >= 10) { cts.Cancel(); break; }
        }

        Assert.Equal(10, ticks.Count);
        Assert.Contains(ticks, t => t.Symbol == "SAP");
        Assert.Contains(ticks, t => t.Symbol == "BMW");
    }

    [Fact]
    public async Task StreamAsync_PricesStayPositive()
    {
        using var cts = new CancellationTokenSource();

        var symbols = new List<SymbolFeedConfig>
        {
            new() { Symbol = "TEST", Exchange = "NYSE", SimulatedBasePrice = 0.05m, SimulatedVolatilityPercent = 0.5m }
        };

        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in _sut.StreamAsync(symbols, cts.Token))
        {
            ticks.Add(tick);
            if (ticks.Count >= 50) { cts.Cancel(); break; }
        }

        Assert.All(ticks, t => Assert.True(t.Price > 0m, $"Price must be positive, was {t.Price}"));
    }

    [Fact]
    public async Task StreamAsync_SymbolAndExchangeAreUpperCase()
    {
        using var cts = new CancellationTokenSource();

        var symbols = new List<SymbolFeedConfig>
        {
            new() { Symbol = "aapl", Exchange = "nyse", SimulatedBasePrice = 200m, SimulatedVolatilityPercent = 0.005m }
        };

        await foreach (var tick in _sut.StreamAsync(symbols, cts.Token))
        {
            Assert.Equal("AAPL", tick.Symbol);
            Assert.Equal("NYSE", tick.Exchange);
            cts.Cancel();
            break;
        }
    }

    [Fact]
    public async Task StreamAsync_PriceVariesAcrossTicks()
    {
        using var cts = new CancellationTokenSource();

        var symbols = new List<SymbolFeedConfig>
        {
            new() { Symbol = "SAP", Exchange = "XETRA", SimulatedBasePrice = 100m, SimulatedVolatilityPercent = 0.05m }
        };

        var prices = new HashSet<decimal>();
        var collected = 0;

        await foreach (var tick in _sut.StreamAsync(symbols, cts.Token))
        {
            prices.Add(tick.Price);
            collected++;
            if (collected >= 20) { cts.Cancel(); break; }
        }

        Assert.True(prices.Count > 1, "Prices should vary across multiple ticks.");
    }
}
