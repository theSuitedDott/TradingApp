namespace TradingApp.TradingEngine.Risk;

/// <summary>
/// Broker-agnostic stop-loss and take-profit evaluator for long positions.
/// </summary>
public sealed class StopLossTakeProfitEvaluator : IPositionRiskEvaluator
{
    /// <inheritdoc />
    public Models.ExitSignal? Evaluate(PositionRiskContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var price = context.Market.Price;
        var position = context.OpenPosition;

        if (position.StopLossPrice is not null && price <= position.StopLossPrice.Value)
        {
            return new Models.ExitSignal(
                Models.TradingAction.Sell,
                Models.ExitReason.StopLoss,
                price,
                $"Price {price} <= stop-loss {position.StopLossPrice.Value}");
        }

        if (position.TakeProfitPrice is not null && price >= position.TakeProfitPrice.Value)
        {
            return new Models.ExitSignal(
                Models.TradingAction.Sell,
                Models.ExitReason.TakeProfit,
                price,
                $"Price {price} >= take-profit {position.TakeProfitPrice.Value}");
        }

        return null;
    }
}
