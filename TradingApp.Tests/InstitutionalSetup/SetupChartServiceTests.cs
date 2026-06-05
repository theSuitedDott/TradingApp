using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Models;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class SetupChartServiceTests
{
    [Fact]
    public async Task GetChartDataAsync_WithMock_ReturnsCandles()
    {
        var sut = CreateSut();

        var result = await sut.GetChartDataAsync("ignored", "1h", "60d", useMock: true);

        Assert.Equal("SAP", result.Symbol);
        Assert.Equal("Mock", result.Source);
        Assert.True(result.Candles.Count > 0);
        Assert.True(result.Candles[0].High >= result.Candles[0].Low);
    }

    [Fact]
    public async Task GetChartDataAsync_WithMockH4_AggregatesCandles()
    {
        var sut = CreateSut();

        var h1 = await sut.GetChartDataAsync("ignored", "1h", "60d", useMock: true);
        var h4 = await sut.GetChartDataAsync("ignored", "4h", "60d", useMock: true);

        Assert.True(h4.Candles.Count <= h1.Candles.Count);
        Assert.Equal("4h", h4.Interval);
    }

    [Fact]
    public async Task GetChartDataAsync_WithIncludeLevels_ReturnsTradeLevelsForMockSetup()
    {
        var sut = CreateSut();

        var result = await sut.GetChartDataAsync("ignored", "1h", "60d", useMock: true, includeTradeLevels: true);

        Assert.NotNull(result.EntryPrice);
        Assert.NotNull(result.StopLossPrice);
        Assert.NotNull(result.TakeProfitPrice);
    }

    [Fact]
    public void ChartLivePriceHelper_UpdatesLastCandleClose()
    {
        var candles = new List<ChartCandleDto>
        {
            new(DateTimeOffset.UtcNow.AddHours(-1), 100m, 101m, 99m, 100m)
        };

        var (updated, price, _) = ChartLivePriceHelper.Apply(candles, 102.5m, DateTimeOffset.UtcNow);

        Assert.Equal(102.5m, price);
        Assert.Equal(102.5m, updated[^1].Close);
        Assert.Equal(102.5m, updated[^1].High);
    }

    private static SetupChartService CreateSut(IMarketQuoteStore? quoteStore = null)
    {
        var settings = Options.Create(new InstitutionalSetupSettings
        {
            Symbol = "SAP",
            Exchange = "XETRA",
            RsiPeriod = 3,
            MaxOpportunities = 10
        });

        return new SetupChartService(
            new StubHistoricalDataService(),
            new MockSetupCandleProvider(),
            new StubScanner(),
            new TradeOpportunityStore(settings),
            quoteStore ?? new MarketQuoteStore(),
            settings);
    }

    private sealed class StubHistoricalDataService : IHistoricalDataService
    {
        public Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
            string symbol, string interval, string range, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());
    }

    private sealed class StubScanner : IInstitutionalSetupScanner
    {
        public SetupAnalysisDto Analyze(string symbol, string exchange)
        {
            var opportunity = new TradeOpportunityDto(
                Guid.NewGuid(),
                symbol,
                exchange,
                "Long",
                "Buy",
                180m,
                175m,
                190m,
                2m,
                1m,
                "Test setup",
                DateTimeOffset.UtcNow,
                "Active",
                Array.Empty<ConditionCheckDto>());

            return new SetupAnalysisDto(symbol, exchange, "Bullish", 1m, true, Array.Empty<ConditionCheckDto>(), opportunity);
        }

        public Task<TradeOpportunityDto?> ScanAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<TradeOpportunityDto?>(null);
    }
}
