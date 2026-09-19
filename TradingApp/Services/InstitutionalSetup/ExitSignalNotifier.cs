using Microsoft.AspNetCore.SignalR;
using TradingApp.DTOs.Setup;
using TradingApp.Hubs;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// SignalR notifier for position exit (sell) alerts.
/// </summary>
public sealed class ExitSignalNotifier(IHubContext<PaperTradingHub> hubContext) : IExitSignalNotifier
{
    /// <inheritdoc />
    public Task NotifyExitSignalAsync(ExitSignalAlertDto alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        return hubContext.Clients
            .Group(PaperTradingHub.AccountGroup(alert.AccountId))
            .SendAsync("ExitSignalDetected", alert, cancellationToken);
    }
}
