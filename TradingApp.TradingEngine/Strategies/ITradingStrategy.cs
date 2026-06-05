namespace TradingApp.TradingEngine.Strategies;

/// <summary>
/// Pluggable trading strategy that emits buy/sell/hold decisions without broker coupling.
/// </summary>
public interface ITradingStrategy
{
    /// <summary>Unique strategy identifier.</summary>
    string StrategyId { get; }

    /// <summary>
    /// Evaluates the strategy for the given context.
    /// </summary>
    /// <param name="context">Market and position context.</param>
    /// <returns>Strategy evaluation including optional stop-loss and take-profit levels.</returns>
    Models.StrategyEvaluation Evaluate(StrategyContext context);
}
