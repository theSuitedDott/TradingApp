using Microsoft.EntityFrameworkCore;
using TradingApp.Entities;

namespace TradingApp.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PaperTradeAccount> PaperTradeAccounts => Set<PaperTradeAccount>();

    public DbSet<BrokerAccount> BrokerAccounts => Set<BrokerAccount>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Strategy> Strategies => Set<Strategy>();

    public DbSet<TradeSignal> TradeSignals => Set<TradeSignal>();

    public DbSet<Backtest> Backtests => Set<Backtest>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MarketQuote> MarketQuotes => Set<MarketQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
