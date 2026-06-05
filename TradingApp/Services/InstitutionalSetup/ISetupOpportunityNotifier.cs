using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Pushes detected institutional setup opportunities to subscribed clients.
/// </summary>
public interface ISetupOpportunityNotifier
{
    /// <summary>Notifies subscribers that a new trade opportunity was detected.</summary>
    Task NotifyOpportunityAsync(TradeOpportunityDto opportunity, CancellationToken cancellationToken = default);
}
