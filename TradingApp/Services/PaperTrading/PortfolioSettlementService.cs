using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.Entities;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Updates cash, positions, and order state when a paper fill occurs.
/// </summary>
public sealed class PortfolioSettlementService(
    ApplicationDbContext dbContext,
    ICommissionCalculator commissionCalculator,
    IPnlCalculator pnlCalculator,
    IPortfolioValuationService valuationService) : IPortfolioSettlementService
{
    /// <inheritdoc />
    public async Task<ServiceResult<Order>> ApplyFillAsync(
        Order order,
        decimal fillPrice,
        decimal fillQuantity,
        CancellationToken cancellationToken = default)
    {
        if (order.PaperTradeAccountId is null)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Order is not linked to a paper trade account.");
        }

        if (fillQuantity <= 0 || fillPrice <= 0)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Fill quantity and price must be positive.");
        }

        var account = await dbContext.PaperTradeAccounts
            .Include(a => a.Portfolio)
            .ThenInclude(p => p.Positions)
            .FirstOrDefaultAsync(a => a.Id == order.PaperTradeAccountId, cancellationToken);

        if (account is null)
        {
            return ServiceResult<Order>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        var portfolio = account.Portfolio;
        var notional = fillPrice * fillQuantity;
        var commission = commissionCalculator.Calculate(notional);
        var now = DateTimeOffset.UtcNow;

        if (order.Side == OrderSide.Buy)
        {
            var totalCost = notional + commission;
            if (portfolio.CashBalance < totalCost)
            {
                order.Status = OrderStatus.Rejected;
                order.RejectReason = "Insufficient cash for fill.";
                order.UpdatedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                return ServiceResult<Order>.Failure(
                    PaperTradingErrorCodes.InsufficientFunds,
                    order.RejectReason);
            }

            portfolio.CashBalance -= totalCost;
            ReleaseReservedCash(portfolio, order);
            await UpsertLongPositionAsync(portfolio, order, fillQuantity, fillPrice, now, cancellationToken);
        }
        else
        {
            var position = portfolio.Positions.FirstOrDefault(p =>
                p.Symbol == order.Symbol &&
                p.Exchange == order.Exchange &&
                p.Status == PositionStatus.Open);

            if (position is null || position.Quantity < fillQuantity)
            {
                order.Status = OrderStatus.Rejected;
                order.RejectReason = "Insufficient position for sell fill.";
                order.UpdatedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                return ServiceResult<Order>.Failure(
                    PaperTradingErrorCodes.InsufficientPosition,
                    order.RejectReason);
            }

            var realized = pnlCalculator.CalculateRealizedLong(
                fillQuantity,
                position.AverageEntryPrice,
                fillPrice);

            position.RealizedPnL += realized;
            position.Quantity -= fillQuantity;

            if (position.Quantity == 0)
            {
                position.Status = PositionStatus.Closed;
                position.ClosedAt = now;
                position.UnrealizedPnL = 0;
            }
            else
            {
                position.CurrentPrice = fillPrice;
                position.UnrealizedPnL = pnlCalculator.CalculateUnrealizedLong(
                    position.Quantity,
                    position.AverageEntryPrice,
                    fillPrice);
            }

            position.UpdatedAt = now;
            portfolio.CashBalance += notional - commission;
        }

        var previousFilled = order.FilledQuantity;
        order.FilledQuantity += fillQuantity;

        if (order.FilledQuantity >= order.Quantity)
        {
            order.Status = OrderStatus.Filled;
            order.FilledAt = now;
        }
        else
        {
            order.Status = OrderStatus.PartiallyFilled;
        }

        order.AverageFillPrice = previousFilled == 0
            ? fillPrice
            : ((order.AverageFillPrice ?? 0) * previousFilled + fillPrice * fillQuantity) / order.FilledQuantity;

        order.Commission += commission;
        order.UpdatedAt = now;

        await valuationService.RecalculateAsync(portfolio.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<Order>.Success(order);
    }

    private async Task UpsertLongPositionAsync(
        Portfolio portfolio,
        Order order,
        decimal fillQuantity,
        decimal fillPrice,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var position = portfolio.Positions.FirstOrDefault(p =>
            p.Symbol == order.Symbol &&
            p.Exchange == order.Exchange &&
            p.Status == PositionStatus.Open);

        if (position is null)
        {
            position = new Position
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolio.Id,
                Symbol = order.Symbol,
                Exchange = order.Exchange,
                Side = OrderSide.Buy,
                Quantity = fillQuantity,
                AverageEntryPrice = fillPrice,
                CurrentPrice = fillPrice,
                UnrealizedPnL = 0,
                RealizedPnL = 0,
                Status = PositionStatus.Open,
                OpenedAt = now,
                UpdatedAt = now
            };
            dbContext.Positions.Add(position);
            portfolio.Positions.Add(position);
        }
        else
        {
            var totalQty = position.Quantity + fillQuantity;
            position.AverageEntryPrice =
                (position.AverageEntryPrice * position.Quantity + fillPrice * fillQuantity) / totalQty;
            position.Quantity = totalQty;
            position.CurrentPrice = fillPrice;
            position.UnrealizedPnL = pnlCalculator.CalculateUnrealizedLong(
                position.Quantity,
                position.AverageEntryPrice,
                fillPrice);
            position.UpdatedAt = now;
        }

        await Task.CompletedTask;
    }

    private static void ReleaseReservedCash(Portfolio portfolio, Order order)
    {
        if (order.LimitPrice is null)
        {
            return;
        }

        var reservedForOrder = order.LimitPrice.Value * order.Quantity;
        portfolio.ReservedCash = Math.Max(0, portfolio.ReservedCash - reservedForOrder);
    }
}
