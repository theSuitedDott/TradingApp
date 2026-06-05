namespace TradingApp.TradingEngine;

/// <summary>
/// Orchestrates multiple strategies and risk evaluation without broker-specific logic.
/// </summary>
public interface ITradingEngine
{
    /// <summary>
    /// Runs all configured strategies and applies stop-loss / take-profit rules.
    /// </summary>
    /// <param name="request">Evaluation request.</param>
    /// <returns>Combined engine result.</returns>
    Models.TradingEngineResult Evaluate(Models.TradingEngineRequest request);
}
