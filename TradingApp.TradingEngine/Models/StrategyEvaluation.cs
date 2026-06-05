namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Output of a single <see cref="Strategies.ITradingStrategy"/> evaluation.
/// </summary>
public sealed class StrategyEvaluation
{
    /// <summary>
    /// Creates a strategy evaluation result.
    /// </summary>
    /// <param name="strategyId">Unique strategy identifier.</param>
    /// <param name="action">Recommended action.</param>
    /// <param name="stopLossPrice">Suggested stop-loss for a new or open long.</param>
    /// <param name="takeProfitPrice">Suggested take-profit for a new or open long.</param>
    /// <param name="reason">Human-readable explanation.</param>
    public StrategyEvaluation(
        string strategyId,
        TradingAction action,
        decimal? stopLossPrice = null,
        decimal? takeProfitPrice = null,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ValidateRiskLevels(action, stopLossPrice, takeProfitPrice);

        StrategyId = strategyId;
        Action = action;
        StopLossPrice = stopLossPrice;
        TakeProfitPrice = takeProfitPrice;
        Reason = reason;
    }

    /// <summary>Strategy identifier.</summary>
    public string StrategyId { get; }

    /// <summary>Recommended trading action.</summary>
    public TradingAction Action { get; }

    /// <summary>Suggested stop-loss price.</summary>
    public decimal? StopLossPrice { get; }

    /// <summary>Suggested take-profit price.</summary>
    public decimal? TakeProfitPrice { get; }

    /// <summary>Optional rationale.</summary>
    public string? Reason { get; }

    private static void ValidateRiskLevels(
        TradingAction action,
        decimal? stopLossPrice,
        decimal? takeProfitPrice)
    {
        if (stopLossPrice is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stopLossPrice));
        }

        if (takeProfitPrice is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(takeProfitPrice));
        }

        if (action == TradingAction.Buy &&
            stopLossPrice is not null &&
            takeProfitPrice is not null &&
            stopLossPrice >= takeProfitPrice)
        {
            throw new ArgumentException("Stop-loss must be below take-profit for long entries.");
        }
    }
}
