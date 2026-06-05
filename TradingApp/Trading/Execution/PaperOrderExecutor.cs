using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Trading.Execution;

/// <summary>
/// Simulated order execution for paper trading without broker APIs.
/// </summary>
public sealed class PaperOrderExecutor(IPortfolioSettlementService settlementService) : IOrderExecutor
{
    /// <inheritdoc />
    public ExecutionVenue Venue => ExecutionVenue.Paper;

    /// <inheritdoc />
    public async Task<ServiceResult<Order>> TryExecuteAsync(
        Order order,
        decimal marketPrice,
        CancellationToken cancellationToken = default)
    {
        if (order.PaperTradeAccountId is null)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Not a paper trade order.");
        }

        if (order.Status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Order is already terminal.");
        }

        var remaining = order.Quantity - order.FilledQuantity;
        if (remaining <= 0)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Order has no remaining quantity.");
        }

        if (!CanFill(order, marketPrice))
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Market price does not satisfy order conditions.");
        }

        var fillPrice = ResolveFillPrice(order, marketPrice);
        return await settlementService.ApplyFillAsync(order, fillPrice, remaining, cancellationToken);
    }

    private static bool CanFill(Order order, decimal marketPrice)
    {
        return order.Type switch
        {
            OrderType.Market => true,
            OrderType.Limit when order.Side == OrderSide.Buy =>
                order.LimitPrice is not null && marketPrice <= order.LimitPrice.Value,
            OrderType.Limit when order.Side == OrderSide.Sell =>
                order.LimitPrice is not null && marketPrice >= order.LimitPrice.Value,
            _ => false
        };
    }

    private static decimal ResolveFillPrice(Order order, decimal marketPrice) =>
        order.Type == OrderType.Limit && order.LimitPrice.HasValue
            ? order.LimitPrice.Value
            : marketPrice;
}
