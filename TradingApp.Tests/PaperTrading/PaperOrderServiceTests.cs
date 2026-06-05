using TradingApp.DTOs.Paper;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;
using TradingApp.Tests.Infrastructure;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class PaperOrderServiceTests
{
    [Fact]
    public async Task PlaceMarketBuyOrder_DeductsCashAndOpensPosition()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var account = await fixture.AccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest { Name = "Trading", InitialBalance = 10_000m });
        var accountId = account.Value!.Id;

        fixture.QuoteStore.SetPrice("SAP", "XETRA", 100m, DateTimeOffset.UtcNow);

        var order = await fixture.OrderService.PlaceOrderAsync(
            userId,
            accountId,
            new PlacePaperOrderRequest
            {
                Symbol = "SAP",
                Exchange = "XETRA",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Quantity = 10m
            });

        Assert.True(order.IsSuccess);
        Assert.Equal("Filled", order.Value!.Status);

        var portfolio = await fixture.AccountService.GetPortfolioSummaryAsync(userId, accountId);
        Assert.True(portfolio.Value!.CashBalance < 10_000m);

        var positions = await fixture.OrderService.GetPositionsAsync(userId, accountId, openOnly: true);
        Assert.Single(positions.Value!);
        Assert.Equal(10m, positions.Value![0].Quantity);
    }

    [Fact]
    public async Task LimitBuy_FillsWhenQuoteCrossesLimit()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var account = await fixture.AccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest { Name = "Limit", InitialBalance = 10_000m });
        var accountId = account.Value!.Id;

        fixture.QuoteStore.SetPrice("SAP", "XETRA", 105m, DateTimeOffset.UtcNow);

        var placed = await fixture.OrderService.PlaceOrderAsync(
            userId,
            accountId,
            new PlacePaperOrderRequest
            {
                Symbol = "SAP",
                Exchange = "XETRA",
                Side = OrderSide.Buy,
                Type = OrderType.Limit,
                Quantity = 5m,
                LimitPrice = 100m
            });

        Assert.True(placed.IsSuccess);
        Assert.Equal("Submitted", placed.Value!.Status);

        var process = await fixture.QuoteProcessor.ProcessQuoteAsync(
            new MarketQuoteTick("SAP", "XETRA", 99m, DateTimeOffset.UtcNow));

        Assert.True(process.IsSuccess);
        Assert.Equal(1, process.Value!.FilledOrdersCount);

        var orders = await fixture.OrderService.GetOrdersAsync(userId, accountId);
        Assert.Equal("Filled", orders.Value![0].Status);
    }

    [Fact]
    public async Task MarketSell_RealizesProfit()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var account = await fixture.AccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest { Name = "Sell", InitialBalance = 10_000m });
        var accountId = account.Value!.Id;

        fixture.QuoteStore.SetPrice("SAP", "XETRA", 100m, DateTimeOffset.UtcNow);
        await fixture.OrderService.PlaceOrderAsync(
            userId,
            accountId,
            new PlacePaperOrderRequest
            {
                Symbol = "SAP",
                Exchange = "XETRA",
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Quantity = 10m
            });

        fixture.QuoteStore.SetPrice("SAP", "XETRA", 120m, DateTimeOffset.UtcNow);
        var sell = await fixture.OrderService.PlaceOrderAsync(
            userId,
            accountId,
            new PlacePaperOrderRequest
            {
                Symbol = "SAP",
                Exchange = "XETRA",
                Side = OrderSide.Sell,
                Type = OrderType.Market,
                Quantity = 10m
            });

        Assert.True(sell.IsSuccess);

        var portfolio = await fixture.AccountService.GetPortfolioSummaryAsync(userId, accountId);
        Assert.True(portfolio.Value!.RealizedPnL > 0);
    }
}
