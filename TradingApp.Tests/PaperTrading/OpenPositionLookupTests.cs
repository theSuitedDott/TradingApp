using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class OpenPositionLookupTests
{
    [Fact]
    public async Task HasOpenPositionAsync_ReturnsTrue_WhenOpenPositionExists()
    {
        await using var db = CreateDb();
        db.Positions.Add(CreateOpenPosition("EUR_USD", "OANDA"));
        await db.SaveChangesAsync();

        var sut = new OpenPositionLookup(db);
        var result = await sut.HasOpenPositionAsync("EUR_USD", "OANDA");

        Assert.True(result);
    }

    [Fact]
    public async Task HasOpenPositionAsync_ReturnsFalse_WhenOnlyClosedPositionExists()
    {
        await using var db = CreateDb();
        var closed = CreateOpenPosition("EUR_USD", "OANDA");
        closed.Status = PositionStatus.Closed;
        closed.ClosedAt = DateTimeOffset.UtcNow;
        db.Positions.Add(closed);
        await db.SaveChangesAsync();

        var sut = new OpenPositionLookup(db);
        var result = await sut.HasOpenPositionAsync("EUR_USD", "OANDA");

        Assert.False(result);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Position CreateOpenPosition(string symbol, string exchange)
    {
        var portfolioId = Guid.NewGuid();
        return new Position
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Portfolio = new Portfolio
            {
                Id = portfolioId,
                PaperTradeAccountId = Guid.NewGuid(),
                CashBalance = 10_000m,
                ReservedCash = 0m,
                TotalEquity = 10_000m,
                BaseCurrency = "EUR",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Symbol = symbol,
            Exchange = exchange,
            Side = OrderSide.Buy,
            Quantity = 1_000m,
            AverageEntryPrice = 1.08m,
            StopLossPrice = 1.07m,
            TakeProfitPrice = 1.10m,
            OpenedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
