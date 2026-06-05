namespace TradingApp.TradingEngine.Aggregation;

/// <summary>
/// Combines multiple strategy evaluations into a single consensus signal.
/// </summary>
public interface ITradingSignalAggregator
{
    /// <summary>
    /// Aggregates strategy evaluations.
    /// </summary>
    /// <param name="evaluations">Non-empty strategy results.</param>
    /// <param name="hasOpenPosition">Whether a position is already open.</param>
    /// <returns>Consensus evaluation or <c>null</c> when no action is agreed.</returns>
    Models.StrategyEvaluation? Aggregate(
        IReadOnlyList<Models.StrategyEvaluation> evaluations,
        bool hasOpenPosition);
}
