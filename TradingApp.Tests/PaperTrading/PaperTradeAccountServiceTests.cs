using TradingApp.DTOs.Paper;
using TradingApp.Tests.Infrastructure;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class PaperTradeAccountServiceTests
{
    [Fact]
    public async Task CreateAccountAsync_InitializesVirtualBalance()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var result = await fixture.AccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest { Name = "Test", InitialBalance = 50_000m });

        Assert.True(result.IsSuccess);
        var portfolio = await fixture.AccountService.GetPortfolioSummaryAsync(userId, result.Value!.Id);
        Assert.True(portfolio.IsSuccess);
        Assert.Equal(50_000m, portfolio.Value!.CashBalance);
        Assert.Equal(50_000m, portfolio.Value.TotalEquity);
    }
}
