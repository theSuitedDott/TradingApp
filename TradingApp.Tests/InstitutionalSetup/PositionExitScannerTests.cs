using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TradingApp.Data;
using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class PositionExitScannerTests
{
    [Fact]
    public async Task ScanAsync_EmitsStopLossAlert_WhenPriceBelowStop()
    {
        await using var db = CreateDb();
        var accountId = Guid.NewGuid();
        var position = CreateOpenPosition(accountId, stopLoss: 1.07m, takeProfit: 1.12m);
        db.Positions.Add(position);
        await db.SaveChangesAsync();

        var quoteStore = new MarketQuoteStore();
        quoteStore.SetPrice(position.Symbol, position.Exchange, 1.065m, DateTimeOffset.UtcNow);

        var notifier = new RecordingExitNotifier();
        var sut = new PositionExitScanner(
            db,
            quoteStore,
            new PositionRiskMonitor(),
            new ExitAlertTracker(),
            notifier,
            NullLogger<PositionExitScanner>.Instance);

        var alerts = await sut.ScanAsync();

        Assert.Single(alerts);
        Assert.Equal("StopLoss", alerts[0].Reason);
        Assert.Equal(1, notifier.Count);
    }

    [Fact]
    public async Task ScanAsync_DoesNotRepeatSameAlert()
    {
        await using var db = CreateDb();
        var accountId = Guid.NewGuid();
        var position = CreateOpenPosition(accountId, stopLoss: 1.07m, takeProfit: 1.12m);
        db.Positions.Add(position);
        await db.SaveChangesAsync();

        var quoteStore = new MarketQuoteStore();
        quoteStore.SetPrice(position.Symbol, position.Exchange, 1.065m, DateTimeOffset.UtcNow);

        var notifier = new RecordingExitNotifier();
        var tracker = new ExitAlertTracker();
        var sut = new PositionExitScanner(
            db,
            quoteStore,
            new PositionRiskMonitor(),
            tracker,
            notifier,
            NullLogger<PositionExitScanner>.Instance);

        await sut.ScanAsync();
        var second = await sut.ScanAsync();

        Assert.Empty(second);
        Assert.Equal(1, notifier.Count);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Position CreateOpenPosition(Guid accountId, decimal stopLoss, decimal takeProfit)
    {
        var portfolioId = Guid.NewGuid();
        return new Position
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Portfolio = new Portfolio
            {
                Id = portfolioId,
                PaperTradeAccountId = accountId,
                CashBalance = 10_000m,
                ReservedCash = 0m,
                TotalEquity = 10_000m,
                BaseCurrency = "EUR",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Symbol = "EUR_USD",
            Exchange = "OANDA",
            Side = OrderSide.Buy,
            Quantity = 1_000m,
            AverageEntryPrice = 1.08m,
            StopLossPrice = stopLoss,
            TakeProfitPrice = takeProfit,
            OpenedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class RecordingExitNotifier : IExitSignalNotifier
    {
        public int Count { get; private set; }

        public Task NotifyExitSignalAsync(
            DTOs.Setup.ExitSignalAlertDto alert,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.CompletedTask;
        }
    }
}
