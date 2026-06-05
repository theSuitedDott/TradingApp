using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

/// <summary>
/// Connection to an external broker for live trading (prepared; enabled only after compliance go-live).
/// </summary>
public class BrokerAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public required string BrokerName { get; set; }

    /// <summary>Broker-side account identifier (never store broker passwords here).</summary>
    public string? ExternalAccountId { get; set; }

    public string BaseCurrency { get; set; } = "EUR";

    public bool IsLiveTradingEnabled { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = [];
}
