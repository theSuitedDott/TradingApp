using TradingApp.DTOs.Paper;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;
using TradingApp.Tests.Infrastructure;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class StopLossExitTests
{
    [Fact]
    public async Task QuoteAtStopLoss_AutomaticallyClosesPosition()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var account = await fixture.AccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest { Name = "SL", InitialBalance = 10_000m });
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
                Quantity = 10m,
                StopLossPrice = 90m
            });

        await fixture.QuoteProcessor.ProcessQuoteAsync(
            new MarketQuoteTick("SAP", "XETRA", 89m, DateTimeOffset.UtcNow));

        var positions = await fixture.OrderService.GetPositionsAsync(userId, accountId, openOnly: true);
        Assert.Empty(positions.Value!);
    }
}
