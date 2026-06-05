using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Paper;
using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Trading.Execution;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Closes paper positions when stop-loss or take-profit levels are hit.
/// </summary>
public sealed class PaperRiskExitService(
    ApplicationDbContext dbContext,
    IOrderExecutorFactory executorFactory,
    IPaperTradingNotifier notifier) : IPaperRiskExitService
{
    /// <inheritdoc />
    public async Task<ServiceResult<OrderResponse>> ExecuteRiskExitAsync(
        RiskExitSignal exit,
        decimal marketPrice,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            PaperTradeAccountId = exit.PaperTradeAccountId,
            Symbol = exit.Symbol,
            Exchange = exit.Exchange,
            Side = OrderSide.Sell,
            Type = OrderType.Market,
            Status = OrderStatus.Pending,
            Quantity = exit.Quantity,
            CreatedAt = now,
            UpdatedAt = now,
            SubmittedAt = now
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var executor = executorFactory.GetExecutor(ExecutionVenue.Paper);
        var result = await executor.TryExecuteAsync(order, marketPrice, cancellationToken);
        if (!result.IsSuccess)
        {
            return ServiceResult<OrderResponse>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        await notifier.NotifyOrderUpdatedAsync(exit.PaperTradeAccountId, result.Value!, cancellationToken);
        await notifier.NotifyPortfolioUpdatedAsync(exit.PaperTradeAccountId, cancellationToken);

        return ServiceResult<OrderResponse>.Success(PaperTradingMapper.ToOrderResponse(result.Value!));
    }
}
