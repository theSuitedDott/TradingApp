namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Combined outcome of strategy evaluation and risk checks.
/// </summary>
public sealed class TradingEngineResult
{
    /// <summary>
    /// Creates a trading engine result.
    /// </summary>
    /// <param name="strategyEvaluations">Per-strategy results.</param>
    /// <param name="aggregatedSignal">Optional consensus entry/exit from strategies.</param>
    /// <param name="riskExit">Optional stop-loss or take-profit exit (takes precedence).</param>
    public TradingEngineResult(
        IReadOnlyList<StrategyEvaluation> strategyEvaluations,
        StrategyEvaluation? aggregatedSignal = null,
        ExitSignal? riskExit = null)
    {
        ArgumentNullException.ThrowIfNull(strategyEvaluations);
        StrategyEvaluations = strategyEvaluations;
        AggregatedSignal = aggregatedSignal;
        RiskExit = riskExit;
    }

    /// <summary>Individual strategy outputs.</summary>
    public IReadOnlyList<StrategyEvaluation> StrategyEvaluations { get; }

    /// <summary>Aggregated strategy signal when applicable.</summary>
    public StrategyEvaluation? AggregatedSignal { get; }

    /// <summary>Risk-based exit; when set, should override discretionary entries.</summary>
    public ExitSignal? RiskExit { get; }

    /// <summary>Effective action after applying risk precedence.</summary>
    public TradingAction EffectiveAction =>
        RiskExit?.Action ?? AggregatedSignal?.Action ?? TradingAction.None;
}
