using TradingApp.Entities;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Pushes realtime paper trading updates to connected clients.
/// </summary>
public interface IPaperTradingNotifier
{
    /// <summary>Notifies subscribers that an order changed.</summary>
    Task NotifyOrderUpdatedAsync(Guid accountId, Order order, CancellationToken cancellationToken = default);

    /// <summary>Notifies subscribers that portfolio metrics changed.</summary>
    Task NotifyPortfolioUpdatedAsync(Guid accountId, CancellationToken cancellationToken = default);
}
