namespace TradingApp.Entities;

/// <summary>
/// Holdings container bound 1:1 to exactly one paper or broker account.
/// </summary>
public class Portfolio
{
    public Guid Id { get; set; }

    public Guid? PaperTradeAccountId { get; set; }

    public PaperTradeAccount? PaperTradeAccount { get; set; }

    public Guid? BrokerAccountId { get; set; }

    public BrokerAccount? BrokerAccount { get; set; }

    public decimal CashBalance { get; set; }

    public decimal ReservedCash { get; set; }

    public decimal TotalEquity { get; set; }

    public string BaseCurrency { get; set; } = "EUR";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Position> Positions { get; set; } = [];
}
