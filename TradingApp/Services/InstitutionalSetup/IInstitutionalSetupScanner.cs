using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Evaluates the institutional setup and emits trade opportunities.
/// </summary>
public interface IInstitutionalSetupScanner
{
    /// <summary>
    /// Runs the full setup analysis for a symbol and returns the condition checklist
    /// (does not store or broadcast).
    /// </summary>
    /// <param name="symbol">Instrument symbol.</param>
    /// <param name="exchange">Exchange / venue.</param>
    /// <returns>The analysis including any resulting opportunity.</returns>
    SetupAnalysisDto Analyze(string symbol, string exchange);

    /// <summary>
    /// Scans the configured symbol; when a new setup is found it is stored and broadcast.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly emitted opportunity, or <c>null</c> when none was emitted.</returns>
    Task<TradeOpportunityDto?> ScanAsync(CancellationToken cancellationToken = default);
}
