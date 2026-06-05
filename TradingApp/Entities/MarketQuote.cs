namespace TradingApp.Entities;

/// <summary>
/// Latest market price for an instrument, updated by realtime quote processing.
/// </summary>
public class MarketQuote
{
    public Guid Id { get; set; }

    public required string Symbol { get; set; }

    public required string Exchange { get; set; }

    public decimal Price { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
