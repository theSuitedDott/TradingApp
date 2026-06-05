namespace TradingApp.Entities;

public class Strategy
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    /// <summary>Strategy parameters as JSON (rules, indicators, thresholds).</summary>
    public string ParametersJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TradeSignal> TradeSignals { get; set; } = [];

    public ICollection<Backtest> Backtests { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];
}
