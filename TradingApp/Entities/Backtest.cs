using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

public class Backtest
{
    public Guid Id { get; set; }

    public Guid StrategyId { get; set; }

    public Strategy Strategy { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal InitialCapital { get; set; }

    public string BaseCurrency { get; set; } = "EUR";

    public BacktestStatus Status { get; set; } = BacktestStatus.Queued;

    /// <summary>Snapshot of strategy parameters used for reproducibility.</summary>
    public string StrategySnapshotJson { get; set; } = "{}";

    /// <summary>Performance metrics (Sharpe, max drawdown, win rate, …) as JSON.</summary>
    public string? ResultsJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
