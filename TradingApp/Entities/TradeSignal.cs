using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

public class TradeSignal
{
    public Guid Id { get; set; }

    public Guid StrategyId { get; set; }

    public Strategy Strategy { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Symbol { get; set; }

    public required string Exchange { get; set; }

    public SignalType SignalType { get; set; }

    public SignalStatus Status { get; set; } = SignalStatus.Active;

    /// <summary>Confidence score 0.0 – 1.0 (e.g. from rules or AI).</summary>
    public decimal Confidence { get; set; }

    public string? Rationale { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
