namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Evaluates the multi-condition institutional setup ("institutional cutting") and
/// produces a trade opportunity when every condition is satisfied.
/// </summary>
public interface IInstitutionalSetupStrategy
{
    /// <summary>
    /// Evaluates setup conditions in pipeline order; stops when an earlier condition fails
    /// (used for live scans and trade opportunities).
    /// </summary>
    /// <param name="input">Multi-timeframe and cross-asset input.</param>
    /// <returns>The aggregated result including the condition checklist.</returns>
    InstitutionalSetupResult Evaluate(InstitutionalSetupInput input);

    /// <summary>
    /// Evaluates every condition independently for chart/backtest visualization.
    /// A trade still requires all six to pass; this mode only changes which checks run.
    /// </summary>
    /// <param name="input">Multi-timeframe and cross-asset input.</param>
    /// <returns>Result with each condition actually tested (no "not evaluated" placeholders).</returns>
    InstitutionalSetupResult EvaluateAllConditions(InstitutionalSetupInput input);
}
