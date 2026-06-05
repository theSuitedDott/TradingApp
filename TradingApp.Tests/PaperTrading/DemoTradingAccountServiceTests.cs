using TradingApp.DTOs.Paper;
using TradingApp.Entities;
using TradingApp.Services.PaperTrading;
using TradingApp.Tests.Infrastructure;
using Xunit;

namespace TradingApp.Tests.PaperTrading;

public sealed class DemoTradingAccountServiceTests
{
    [Fact]
    public async Task EnsureDefaultPaperAccountAsync_CreatesStarterAccount_WhenNoneExists()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        fixture.DbContext.Users.Add(new User
        {
            Id = userId,
            Email = "demo@test.local",
            PasswordHash = "hash",
            Role = UserRole.Viewer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await fixture.DbContext.SaveChangesAsync();

        var service = new DemoTradingAccountService(fixture.DbContext, fixture.AccountService);
        var result = await service.EnsureDefaultPaperAccountAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(100_000m, result.Value!.InitialBalance);
        Assert.Equal("EUR", result.Value.BaseCurrency);

        var user = await fixture.DbContext.Users.FindAsync(userId);
        Assert.Equal(UserRole.Trader, user!.Role);
    }

    [Fact]
    public async Task EnsureDefaultPaperAccountAsync_IsIdempotent()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var service = new DemoTradingAccountService(fixture.DbContext, fixture.AccountService);

        var first = await service.EnsureDefaultPaperAccountAsync(userId);
        var second = await service.EnsureDefaultPaperAccountAsync(userId);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
    }

    [Fact]
    public async Task LinkDemoBrokerAsync_RequiresExternalId_ForIbPaper()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var service = new DemoTradingAccountService(fixture.DbContext, fixture.AccountService);

        var result = await service.LinkDemoBrokerAsync(
            userId,
            new LinkDemoBrokerRequest { PresetId = "ib-paper" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LinkDemoBrokerAsync_CreatesSimulatedConnection()
    {
        using var fixture = new PaperTradingTestFixture();
        var userId = Guid.NewGuid();
        var service = new DemoTradingAccountService(fixture.DbContext, fixture.AccountService);

        var result = await service.LinkDemoBrokerAsync(
            userId,
            new LinkDemoBrokerRequest { PresetId = "simulated-feed" });

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsLiveTradingEnabled);
        Assert.Equal("Demo", result.Value.ConnectionMode);
    }
}
