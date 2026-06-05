using TradingApp.Services.PaperTrading;
using TradingApp.Tests.Infrastructure;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class StopLossTakeProfitIntegrationTests
{
    [Fact]
    public async Task QuoteUpdate_RecalculatesUnrealizedPnL()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var account = await fixture.AccountService.CreateAccountAsync(
            userId,
            new DTOs.Paper.CreatePaperAccountRequest { Name = "PnL", InitialBalance = 10_000m });

        fixture.QuoteStore.SetPrice("SAP", "XETRA", 100m, DateTimeOffset.UtcNow);
        await fixture.OrderService.PlaceOrderAsync(
            userId,
            account.Value!.Id,
            new DTOs.Paper.PlacePaperOrderRequest
            {
                Symbol = "SAP",
                Exchange = "XETRA",
                Side = Entities.Enums.OrderSide.Buy,
                Type = Entities.Enums.OrderType.Market,
                Quantity = 10m
            });

        await fixture.QuoteProcessor.ProcessQuoteAsync(
            new MarketQuoteTick("SAP", "XETRA", 110m, DateTimeOffset.UtcNow));

        var portfolio = await fixture.AccountService.GetPortfolioSummaryAsync(userId, account.Value.Id);
        Assert.Equal(100m, portfolio.Value!.UnrealizedPnL);
        Assert.True(portfolio.Value.TotalEquity > 10_000m);
    }
}
