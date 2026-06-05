using TradingApp.TradingEngine.Aggregation;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Risk;
using TradingApp.TradingEngine.Strategies;

namespace TradingApp.TradingEngine;

/// <summary>
/// Default trading engine: evaluates strategies, aggregates signals, then applies risk exits.
/// </summary>
public sealed class TradingEngineService : ITradingEngine
{
    private readonly IPositionRiskEvaluator _riskEvaluator;
    private readonly ITradingSignalAggregator _signalAggregator;

    /// <summary>
    /// Creates the trading engine with injected dependencies.
    /// </summary>
    /// <param name="riskEvaluator">Stop-loss / take-profit evaluator.</param>
    /// <param name="signalAggregator">Strategy consensus aggregator.</param>
    public TradingEngineService(
        IPositionRiskEvaluator riskEvaluator,
        ITradingSignalAggregator signalAggregator)
    {
        ArgumentNullException.ThrowIfNull(riskEvaluator);
        ArgumentNullException.ThrowIfNull(signalAggregator);
        _riskEvaluator = riskEvaluator;
        _signalAggregator = signalAggregator;
    }

    /// <inheritdoc />
    public TradingEngineResult Evaluate(TradingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = new StrategyContext(request.Market, request.OpenPosition);
        var evaluations = request.Strategies
            .Select(strategy => strategy.Evaluate(context))
            .ToList();

        ExitSignal? riskExit = null;
        if (request.OpenPosition is not null)
        {
            var riskContext = new PositionRiskContext(request.Market, request.OpenPosition);
            riskExit = _riskEvaluator.Evaluate(riskContext);
        }

        var aggregated = riskExit is null
            ? _signalAggregator.Aggregate(evaluations, request.OpenPosition is not null)
            : null;

        return new TradingEngineResult(evaluations, aggregated, riskExit);
    }
}
