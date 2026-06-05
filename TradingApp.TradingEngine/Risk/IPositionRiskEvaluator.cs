namespace TradingApp.TradingEngine.Risk;

/// <summary>
/// Evaluates absolute stop-loss and take-profit levels for an open position.
/// </summary>
public interface IPositionRiskEvaluator
{
    /// <summary>
    /// Determines whether the current price triggers a risk exit.
    /// </summary>
    /// <param name="context">Market and position context.</param>
    /// <returns>Exit signal when stop-loss or take-profit is hit; otherwise <c>null</c>.</returns>
    Models.ExitSignal? Evaluate(PositionRiskContext context);
}
