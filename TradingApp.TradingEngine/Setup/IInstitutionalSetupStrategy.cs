namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Evaluates the multi-condition institutional setup ("institutional cutting") and
/// produces a trade opportunity when every condition is satisfied.
/// </summary>
public interface IInstitutionalSetupStrategy
{
    /// <summary>
    /// Evaluates all setup conditions for the given input.
    /// </summary>
    /// <param name="input">Multi-timeframe and cross-asset input.</param>
    /// <returns>The aggregated result including the condition checklist.</returns>
    InstitutionalSetupResult Evaluate(InstitutionalSetupInput input);
}
