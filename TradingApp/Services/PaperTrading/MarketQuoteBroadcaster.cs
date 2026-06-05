using Microsoft.AspNetCore.SignalR;
using TradingApp.Hubs;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// SignalR broadcaster for live chart quote updates.
/// </summary>
public sealed class MarketQuoteBroadcaster(IHubContext<PaperTradingHub> hubContext) : IMarketQuoteBroadcaster
{
    /// <inheritdoc />
    public Task BroadcastQuoteAsync(MarketQuoteTick tick, CancellationToken cancellationToken = default)
    {
        var group = PaperTradingHub.QuoteGroup(tick.Symbol, tick.Exchange);
        return hubContext.Clients.Group(group).SendAsync(
            "QuoteUpdated",
            new
            {
                tick.Symbol,
                tick.Exchange,
                tick.Price,
                tick.Timestamp
            },
            cancellationToken);
    }
}
