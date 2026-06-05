using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

public class Position
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public Portfolio Portfolio { get; set; } = null!;

    public required string Symbol { get; set; }

    public required string Exchange { get; set; }

    public string? Isin { get; set; }

    public OrderSide Side { get; set; }

    public decimal Quantity { get; set; }

    public decimal AverageEntryPrice { get; set; }

    public decimal? CurrentPrice { get; set; }

    /// <summary>Absolute stop-loss trigger (long).</summary>
    public decimal? StopLossPrice { get; set; }

    /// <summary>Absolute take-profit trigger (long).</summary>
    public decimal? TakeProfitPrice { get; set; }

    public decimal UnrealizedPnL { get; set; }

    public decimal RealizedPnL { get; set; }

    public PositionStatus Status { get; set; } = PositionStatus.Open;

    public DateTimeOffset OpenedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
