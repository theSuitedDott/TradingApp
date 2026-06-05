namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Point-in-time market data used for strategy evaluation (broker-agnostic).
/// </summary>
public sealed class MarketSnapshot
{
    /// <summary>
    /// Initializes a new market snapshot.
    /// </summary>
    /// <param name="symbol">Instrument symbol.</param>
    /// <param name="price">Current or last traded price.</param>
    /// <param name="timestamp">Quote or bar timestamp (UTC).</param>
    /// <param name="priceHistory">Optional prior prices (oldest first) for indicator strategies.</param>
    public MarketSnapshot(
        string symbol,
        decimal price,
        DateTimeOffset timestamp,
        IReadOnlyList<decimal>? priceHistory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price must be positive.");
        }

        Symbol = symbol;
        Price = price;
        Timestamp = timestamp;
        PriceHistory = priceHistory ?? [];
    }

    /// <summary>Instrument symbol.</summary>
    public string Symbol { get; }

    /// <summary>Current or last price.</summary>
    public decimal Price { get; }

    /// <summary>Timestamp of the snapshot (UTC).</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Historical prices including <see cref="Price"/> as the latest value when provided.</summary>
    public IReadOnlyList<decimal> PriceHistory { get; }
}
