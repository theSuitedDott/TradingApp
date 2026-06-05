namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Broadcasts live market quote ticks to connected chart clients.
/// </summary>
public interface IMarketQuoteBroadcaster
{
    /// <summary>Pushes a quote update to subscribers of the symbol/exchange pair.</summary>
    Task BroadcastQuoteAsync(MarketQuoteTick tick, CancellationToken cancellationToken = default);
}
