using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Broadcasts sell alerts to connected clients.
/// </summary>
public interface IExitSignalNotifier
{
    /// <summary>Sends a sell alert to the paper account group.</summary>
    Task NotifyExitSignalAsync(ExitSignalAlertDto alert, CancellationToken cancellationToken = default);
}
