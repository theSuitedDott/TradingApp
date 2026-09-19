using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Runs historical setup backtests over real market data.
/// </summary>
public interface ISetupBacktestService
{
    /// <summary>
    /// Evaluates the institutional setup over the last 60 days (OANDA forex, Yahoo indices).
    /// </summary>
    /// <param name="symbol">OANDA instrument (e.g. EUR_USD, GBP_USD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Partial and complete setups with at least 4 of 6 conditions met.</returns>
    Task<IReadOnlyList<SetupAnalysisDto>> RunAsync(string symbol, CancellationToken cancellationToken = default);
}
