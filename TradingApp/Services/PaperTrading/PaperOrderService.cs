using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Paper;
using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Trading.Execution;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Orchestrates virtual order placement via the paper execution port.
/// </summary>
public sealed class PaperOrderService(
    ApplicationDbContext dbContext,
    IMarketQuoteStore quoteStore,
    IOrderExecutorFactory executorFactory,
    ICommissionCalculator commissionCalculator,
    IPaperTradingNotifier notifier) : IPaperOrderService
{
    /// <inheritdoc />
    public async Task<ServiceResult<OrderResponse>> PlaceOrderAsync(
        Guid userId,
        Guid accountId,
        PlacePaperOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        if (account.Status != AccountStatus.Active)
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.AccountInactive,
                "Paper trade account is not active.");
        }

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            var duplicate = await dbContext.Orders.AnyAsync(o =>
                o.ClientOrderId == request.ClientOrderId, cancellationToken);

            if (duplicate)
            {
                return ServiceResult<OrderResponse>.Failure(
                    PaperTradingErrorCodes.DuplicateClientOrderId,
                    "Client order id already exists.");
            }
        }

        if (request.Type == OrderType.Limit && request.LimitPrice is null)
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Limit orders require a limit price.");
        }

        if (request.Side == OrderSide.Sell)
        {
            var positionQty = account.Portfolio.Positions
                .Where(p =>
                    p.Symbol == request.Symbol.Trim().ToUpperInvariant() &&
                    p.Exchange == request.Exchange.Trim().ToUpperInvariant() &&
                    p.Status == PositionStatus.Open)
                .Sum(p => p.Quantity);

            if (positionQty < request.Quantity)
            {
                return ServiceResult<OrderResponse>.Failure(
                    PaperTradingErrorCodes.InsufficientPosition,
                    "Insufficient open position for sell order.");
            }
        }

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var exchange = request.Exchange.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        if (request.Side == OrderSide.Buy && request.Type == OrderType.Limit)
        {
            var reserveResult = TryReserveCashForLimitBuy(account.Portfolio, request);
            if (!reserveResult.IsSuccess)
            {
                return ServiceResult<OrderResponse>.Failure(
                    reserveResult.ErrorCode!,
                    reserveResult.ErrorMessage!);
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            PaperTradeAccountId = accountId,
            Symbol = symbol,
            Exchange = exchange,
            Side = request.Side,
            Type = request.Type,
            Status = OrderStatus.Pending,
            Quantity = request.Quantity,
            LimitPrice = request.LimitPrice,
            ClientOrderId = request.ClientOrderId,
            CreatedAt = now,
            UpdatedAt = now,
            SubmittedAt = now
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Type == OrderType.Market)
        {
            var (hasPrice, marketPrice) = await TryResolveMarketPriceAsync(symbol, exchange, cancellationToken);
            if (!hasPrice)
            {
                order.Status = OrderStatus.Rejected;
                order.RejectReason = "No market price available for market order.";
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return ServiceResult<OrderResponse>.Failure(
                    PaperTradingErrorCodes.NoMarketPrice,
                    order.RejectReason);
            }

            var executor = executorFactory.GetExecutor(ExecutionVenue.Paper);
            var execResult = await executor.TryExecuteAsync(order, marketPrice, cancellationToken);
            if (!execResult.IsSuccess)
            {
                return ServiceResult<OrderResponse>.Failure(
                    execResult.ErrorCode!,
                    execResult.ErrorMessage!);
            }

            order = execResult.Value!;
            await ApplyRiskLevelsOnBuyAsync(accountId, order, request, cancellationToken);
            await notifier.NotifyOrderUpdatedAsync(accountId, order, cancellationToken);
            await notifier.NotifyPortfolioUpdatedAsync(accountId, cancellationToken);
        }
        else
        {
            order.Status = OrderStatus.Submitted;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<OrderResponse>.Success(PaperTradingMapper.ToOrderResponse(order));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<OrderResponse>> CancelOrderAsync(
        Guid userId,
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.PaperTradeAccountId == accountId, cancellationToken);

        if (order is null)
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.OrderNotFound,
                "Order not found.");
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Submitted))
        {
            return ServiceResult<OrderResponse>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Only pending orders can be cancelled.");
        }

        if (order.Side == OrderSide.Buy && order.LimitPrice is not null)
        {
            var reserved = order.LimitPrice.Value * order.Quantity;
            account.Portfolio.ReservedCash = Math.Max(0, account.Portfolio.ReservedCash - reserved);
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTimeOffset.UtcNow;
        order.UpdatedAt = order.CancelledAt.Value;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notifier.NotifyOrderUpdatedAsync(accountId, order, cancellationToken);
        return ServiceResult<OrderResponse>.Success(PaperTradingMapper.ToOrderResponse(order));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<OrderResponse>>> GetOrdersAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsAccountAsync(userId, accountId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<OrderResponse>>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.PaperTradeAccountId == accountId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<OrderResponse>>.Success(
            orders.Select(PaperTradingMapper.ToOrderResponse).ToList());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<PositionResponse>>> GetPositionsAsync(
        Guid userId,
        Guid accountId,
        bool openOnly,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.PaperTradeAccounts
            .AsNoTracking()
            .Include(a => a.Portfolio)
            .ThenInclude(p => p.Positions)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

        if (account is null)
        {
            return ServiceResult<IReadOnlyList<PositionResponse>>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        var positions = account.Portfolio.Positions.AsEnumerable();
        if (openOnly)
        {
            positions = positions.Where(p => p.Status == PositionStatus.Open);
        }

        return ServiceResult<IReadOnlyList<PositionResponse>>.Success(
            positions.Select(PaperTradingMapper.ToPositionResponse).ToList());
    }

    private async Task<PaperTradeAccount?> LoadAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.PaperTradeAccounts
            .Include(a => a.Portfolio)
            .ThenInclude(p => p.Positions)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

    private async Task<bool> OwnsAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.PaperTradeAccounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

    private ServiceResult<bool> TryReserveCashForLimitBuy(Portfolio portfolio, PlacePaperOrderRequest request)
    {
        var notional = request.LimitPrice!.Value * request.Quantity;
        var commission = commissionCalculator.Calculate(notional);
        var reserveTotal = notional + commission;
        var available = portfolio.CashBalance - portfolio.ReservedCash;

        if (available < reserveTotal)
        {
            return ServiceResult<bool>.Failure(
                PaperTradingErrorCodes.InsufficientFunds,
                "Insufficient available cash for limit buy.");
        }

        portfolio.ReservedCash += reserveTotal;
        portfolio.UpdatedAt = DateTimeOffset.UtcNow;
        return ServiceResult<bool>.Success(true);
    }

    private async Task ApplyRiskLevelsOnBuyAsync(
        Guid accountId,
        Order order,
        PlacePaperOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (order.Side != OrderSide.Buy ||
            order.Status is not (OrderStatus.Filled or OrderStatus.PartiallyFilled) ||
            (request.StopLossPrice is null && request.TakeProfitPrice is null))
        {
            return;
        }

        var position = await dbContext.Positions.FirstOrDefaultAsync(
            p => p.Portfolio.PaperTradeAccountId == accountId &&
                 p.Symbol == order.Symbol &&
                 p.Exchange == order.Exchange &&
                 p.Status == PositionStatus.Open,
            cancellationToken);

        if (position is null)
        {
            return;
        }

        position.StopLossPrice = request.StopLossPrice;
        position.TakeProfitPrice = request.TakeProfitPrice;
        position.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(bool Found, decimal Price)> TryResolveMarketPriceAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken)
    {
        if (quoteStore.TryGetPrice(symbol, exchange, out var cachedPrice))
        {
            return (true, cachedPrice);
        }

        var quote = await dbContext.MarketQuotes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Symbol == symbol && q.Exchange == exchange, cancellationToken);

        if (quote is null)
        {
            return (false, 0);
        }

        quoteStore.SetPrice(symbol, exchange, quote.Price, quote.Timestamp);
        return (true, quote.Price);
    }
}
