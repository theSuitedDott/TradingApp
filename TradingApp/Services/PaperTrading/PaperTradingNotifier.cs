using Microsoft.AspNetCore.SignalR;
using TradingApp.Entities;
using TradingApp.Hubs;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// SignalR-based realtime notifier for paper trading clients.
/// </summary>
public sealed class PaperTradingNotifier(IHubContext<PaperTradingHub> hubContext) : IPaperTradingNotifier
{
    /// <inheritdoc />
    public Task NotifyOrderUpdatedAsync(Guid accountId, Order order, CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(PaperTradingHub.AccountGroup(accountId))
            .SendAsync("OrderUpdated", new
            {
                order.Id,
                order.Status,
                order.FilledQuantity,
                order.AverageFillPrice,
                order.Symbol,
                order.Exchange
            }, cancellationToken);

    /// <inheritdoc />
    public Task NotifyPortfolioUpdatedAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(PaperTradingHub.AccountGroup(accountId))
            .SendAsync("PortfolioUpdated", new { accountId }, cancellationToken);
}
