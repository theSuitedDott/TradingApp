using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid? PaperTradeAccountId { get; set; }

    public PaperTradeAccount? PaperTradeAccount { get; set; }

    public Guid? BrokerAccountId { get; set; }

    public BrokerAccount? BrokerAccount { get; set; }

    public Guid? StrategyId { get; set; }

    public Strategy? Strategy { get; set; }

    public Guid? TradeSignalId { get; set; }

    public TradeSignal? TradeSignal { get; set; }

    public required string Symbol { get; set; }

    public required string Exchange { get; set; }

    public OrderSide Side { get; set; }

    public OrderType Type { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Quantity { get; set; }

    public decimal FilledQuantity { get; set; }

    public decimal? LimitPrice { get; set; }

    public decimal? StopPrice { get; set; }

    public decimal? AverageFillPrice { get; set; }

    public decimal Commission { get; set; }

    public string? RejectReason { get; set; }

    public string? ClientOrderId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? FilledAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
