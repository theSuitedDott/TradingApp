namespace TradingApp.TradingEngine.Strategies;

/// <summary>
/// Emits a buy when price crosses above an entry threshold and sell when below an exit threshold.
/// Includes configurable stop-loss and take-profit offsets from entry.
/// </summary>
public sealed class ThresholdStrategy : ITradingStrategy
{
    /// <summary>
    /// Creates a threshold strategy.
    /// </summary>
    /// <param name="strategyId">Unique identifier.</param>
    /// <param name="buyAbovePrice">Buy when market price is at or above this level (no open position).</param>
    /// <param name="sellBelowPrice">Sell when price is at or below this level (with open position).</param>
    /// <param name="stopLossPercent">Stop-loss distance as percent below entry (e.g. 2 for 2%).</param>
    /// <param name="takeProfitPercent">Take-profit distance as percent above entry (e.g. 5 for 5%).</param>
    public ThresholdStrategy(
        string strategyId,
        decimal buyAbovePrice,
        decimal sellBelowPrice,
        decimal stopLossPercent = 2m,
        decimal takeProfitPercent = 5m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);

        if (buyAbovePrice <= 0 || sellBelowPrice <= 0)
        {
            throw new ArgumentOutOfRangeException("Threshold prices must be positive.");
        }

        if (stopLossPercent <= 0 || takeProfitPercent <= 0)
        {
            throw new ArgumentOutOfRangeException("Risk percentages must be positive.");
        }

        StrategyId = strategyId;
        BuyAbovePrice = buyAbovePrice;
        SellBelowPrice = sellBelowPrice;
        StopLossPercent = stopLossPercent;
        TakeProfitPercent = takeProfitPercent;
    }

    /// <inheritdoc />
    public string StrategyId { get; }

    /// <summary>Buy threshold price.</summary>
    public decimal BuyAbovePrice { get; }

    /// <summary>Sell threshold price.</summary>
    public decimal SellBelowPrice { get; }

    /// <summary>Stop-loss percent below entry.</summary>
    public decimal StopLossPercent { get; }

    /// <summary>Take-profit percent above entry.</summary>
    public decimal TakeProfitPercent { get; }

    /// <inheritdoc />
    public Models.StrategyEvaluation Evaluate(StrategyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var price = context.Market.Price;

        if (context.HasOpenPosition)
        {
            if (price <= SellBelowPrice)
            {
                return new Models.StrategyEvaluation(
                    StrategyId,
                    Models.TradingAction.Sell,
                    reason: $"Price {price} <= sell threshold {SellBelowPrice}");
            }

            return new Models.StrategyEvaluation(StrategyId, Models.TradingAction.None, reason: "Hold position");
        }

        if (price >= BuyAbovePrice)
        {
            var stopLoss = price * (1m - StopLossPercent / 100m);
            var takeProfit = price * (1m + TakeProfitPercent / 100m);

            return new Models.StrategyEvaluation(
                StrategyId,
                Models.TradingAction.Buy,
                stopLoss,
                takeProfit,
                $"Price {price} >= buy threshold {BuyAbovePrice}");
        }

        return new Models.StrategyEvaluation(StrategyId, Models.TradingAction.None, reason: "Awaiting entry");
    }
}
