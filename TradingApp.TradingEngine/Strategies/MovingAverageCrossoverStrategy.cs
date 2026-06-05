namespace TradingApp.TradingEngine.Strategies;

/// <summary>
/// Simple moving-average crossover: buy on golden cross, sell on death cross with risk levels.
/// </summary>
public sealed class MovingAverageCrossoverStrategy : ITradingStrategy
{
    /// <summary>
    /// Creates a moving-average crossover strategy.
    /// </summary>
    /// <param name="strategyId">Unique identifier.</param>
    /// <param name="fastPeriod">Fast SMA period (must be &lt; slow).</param>
    /// <param name="slowPeriod">Slow SMA period.</param>
    /// <param name="stopLossPercent">Stop-loss percent below entry.</param>
    /// <param name="takeProfitPercent">Take-profit percent above entry.</param>
    public MovingAverageCrossoverStrategy(
        string strategyId,
        int fastPeriod,
        int slowPeriod,
        decimal stopLossPercent = 2m,
        decimal takeProfitPercent = 5m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);

        if (fastPeriod < 2 || slowPeriod < 2 || fastPeriod >= slowPeriod)
        {
            throw new ArgumentException("Require 2 <= fastPeriod < slowPeriod.");
        }

        if (stopLossPercent <= 0 || takeProfitPercent <= 0)
        {
            throw new ArgumentOutOfRangeException("Risk percentages must be positive.");
        }

        StrategyId = strategyId;
        FastPeriod = fastPeriod;
        SlowPeriod = slowPeriod;
        StopLossPercent = stopLossPercent;
        TakeProfitPercent = takeProfitPercent;
    }

    /// <inheritdoc />
    public string StrategyId { get; }

    /// <summary>Fast SMA window.</summary>
    public int FastPeriod { get; }

    /// <summary>Slow SMA window.</summary>
    public int SlowPeriod { get; }

    /// <summary>Stop-loss percent.</summary>
    public decimal StopLossPercent { get; }

    /// <summary>Take-profit percent.</summary>
    public decimal TakeProfitPercent { get; }

    /// <inheritdoc />
    public Models.StrategyEvaluation Evaluate(StrategyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var history = context.Market.PriceHistory;
        if (history.Count < SlowPeriod)
        {
            return new Models.StrategyEvaluation(
                StrategyId,
                Models.TradingAction.None,
                reason: "Insufficient price history");
        }

        var fast = CalculateSma(history, FastPeriod);
        var slow = CalculateSma(history, SlowPeriod);
        var price = context.Market.Price;

        if (!context.HasOpenPosition)
        {
            if (fast > slow)
            {
                var stopLoss = price * (1m - StopLossPercent / 100m);
                var takeProfit = price * (1m + TakeProfitPercent / 100m);
                return new Models.StrategyEvaluation(
                    StrategyId,
                    Models.TradingAction.Buy,
                    stopLoss,
                    takeProfit,
                    $"Fast SMA {fast:F4} > slow SMA {slow:F4}");
            }

            return new Models.StrategyEvaluation(StrategyId, Models.TradingAction.None, reason: "No crossover buy");
        }

        if (fast < slow)
        {
            return new Models.StrategyEvaluation(
                StrategyId,
                Models.TradingAction.Sell,
                reason: $"Fast SMA {fast:F4} < slow SMA {slow:F4}");
        }

        return new Models.StrategyEvaluation(StrategyId, Models.TradingAction.None, reason: "Hold position");
    }

    private static decimal CalculateSma(IReadOnlyList<decimal> prices, int period)
    {
        var slice = prices.TakeLast(period);
        return slice.Average();
    }
}
