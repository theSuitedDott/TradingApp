namespace TradingApp.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<PaperTradeAccount> PaperTradeAccounts { get; set; } = [];

    public ICollection<BrokerAccount> BrokerAccounts { get; set; } = [];

    public ICollection<Strategy> Strategies { get; set; } = [];

    public ICollection<TradeSignal> TradeSignals { get; set; } = [];

    public ICollection<Backtest> Backtests { get; set; } = [];

    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
