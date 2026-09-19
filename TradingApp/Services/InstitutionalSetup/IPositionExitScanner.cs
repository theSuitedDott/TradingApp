using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Periodically checks open positions against live prices for stop-loss / take-profit exits.
/// </summary>
public interface IPositionExitScanner
{
    /// <summary>
    /// Evaluates all open positions and emits one-shot sell alerts when SL/TP is reached.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Newly emitted alerts in this scan (after de-duplication).</returns>
    Task<IReadOnlyList<ExitSignalAlertDto>> ScanAsync(CancellationToken cancellationToken = default);
}
