using Microsoft.AspNetCore.SignalR;
using TradingApp.DTOs.Setup;
using TradingApp.Hubs;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// SignalR-based notifier that broadcasts setup opportunities to the setup group.
/// </summary>
public sealed class SetupOpportunityNotifier(IHubContext<PaperTradingHub> hubContext) : ISetupOpportunityNotifier
{
    /// <inheritdoc />
    public Task NotifyOpportunityAsync(TradeOpportunityDto opportunity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(opportunity);

        return hubContext.Clients
            .Group(PaperTradingHub.SetupGroup)
            .SendAsync("SetupDetected", opportunity, cancellationToken);
    }
}
