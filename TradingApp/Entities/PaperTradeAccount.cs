using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

/// <summary>
/// Simulated trading account for paper trading. Strictly separated from live broker accounts.
/// </summary>
public class PaperTradeAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public string BaseCurrency { get; set; } = "EUR";

    public decimal InitialBalance { get; set; }

    public bool IsDefault { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = [];
}
