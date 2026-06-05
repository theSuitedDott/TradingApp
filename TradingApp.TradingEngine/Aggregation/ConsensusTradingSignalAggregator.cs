using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Aggregation;

/// <summary>
/// Requires a majority of strategies to agree on the same non-none action.
/// </summary>
public sealed class ConsensusTradingSignalAggregator : ITradingSignalAggregator
{
    /// <inheritdoc />
    public StrategyEvaluation? Aggregate(IReadOnlyList<StrategyEvaluation> evaluations, bool hasOpenPosition)
    {
        ArgumentNullException.ThrowIfNull(evaluations);

        if (evaluations.Count == 0)
        {
            return null;
        }

        var preferredAction = hasOpenPosition ? TradingAction.Sell : TradingAction.Buy;
        var matching = evaluations
            .Where(e => e.Action == preferredAction)
            .ToList();

        var required = (evaluations.Count / 2) + 1;
        if (matching.Count < required)
        {
            return null;
        }

        var stopLoss = matching
            .Where(e => e.StopLossPrice.HasValue)
            .Select(e => e.StopLossPrice!.Value)
            .DefaultIfEmpty()
            .Average();

        var takeProfit = matching
            .Where(e => e.TakeProfitPrice.HasValue)
            .Select(e => e.TakeProfitPrice!.Value)
            .DefaultIfEmpty()
            .Average();

        decimal? sl = matching.Any(e => e.StopLossPrice.HasValue) ? stopLoss : null;
        decimal? tp = matching.Any(e => e.TakeProfitPrice.HasValue) ? takeProfit : null;

        if (sl == 0)
        {
            sl = null;
        }

        if (tp == 0)
        {
            tp = null;
        }

        return new StrategyEvaluation(
            "consensus",
            preferredAction,
            sl,
            tp,
            $"{matching.Count}/{evaluations.Count} strategies agree on {preferredAction}");
    }
}
