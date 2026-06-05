using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.Entities;
using TradingApp.Entities.Enums;
using TradingApp.Trading.Execution;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Realtime quote pipeline for paper trading: cache, persist, fill orders, mark-to-market.
/// </summary>
public sealed class MarketQuoteProcessor(
    ApplicationDbContext dbContext,
    IMarketQuoteStore quoteStore,
    IOrderExecutorFactory executorFactory,
    IPortfolioValuationService valuationService,
    IPositionRiskMonitor riskMonitor,
    IPaperRiskExitService riskExitService,
    IPaperTradingNotifier notifier) : IMarketQuoteProcessor
{
    /// <inheritdoc />
    public async Task<ServiceResult<MarketQuoteProcessResult>> ProcessQuoteAsync(
        MarketQuoteTick tick,
        CancellationToken cancellationToken = default)
    {
        if (tick.Price <= 0)
        {
            return ServiceResult<MarketQuoteProcessResult>.Failure(
                PaperTradingErrorCodes.InvalidOrderState,
                "Quote price must be positive.");
        }

        var symbol = tick.Symbol.Trim().ToUpperInvariant();
        var exchange = tick.Exchange.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        quoteStore.SetPrice(symbol, exchange, tick.Price, tick.Timestamp);
        await UpsertQuoteEntityAsync(symbol, exchange, tick.Price, tick.Timestamp, now, cancellationToken);

        var executor = executorFactory.GetExecutor(ExecutionVenue.Paper);
        var pendingOrders = await dbContext.Orders
            .Where(o =>
                o.PaperTradeAccountId != null &&
                o.Symbol == symbol &&
                o.Exchange == exchange &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Submitted))
            .ToListAsync(cancellationToken);

        var filledCount = 0;

        var openPositions = await dbContext.Positions
            .Include(p => p.Portfolio)
            .Where(p =>
                p.Status == PositionStatus.Open &&
                p.Symbol == symbol &&
                p.Exchange == exchange &&
                p.Portfolio.PaperTradeAccountId != null)
            .ToListAsync(cancellationToken);

        foreach (var position in openPositions)
        {
            var accountId = position.Portfolio.PaperTradeAccountId!.Value;
            foreach (var exit in riskMonitor.GetTriggeredExits(position, accountId, tick.Price))
            {
                var exitResult = await riskExitService.ExecuteRiskExitAsync(exit, tick.Price, cancellationToken);
                if (exitResult.IsSuccess)
                {
                    filledCount++;
                }
            }
        }

        foreach (var order in pendingOrders)
        {
            var result = await executor.TryExecuteAsync(order, tick.Price, cancellationToken);
            if (result.IsSuccess)
            {
                filledCount++;
                await notifier.NotifyOrderUpdatedAsync(order.PaperTradeAccountId!.Value, result.Value!, cancellationToken);
            }
        }

        await valuationService.RecalculateByInstrumentAsync(symbol, exchange, tick.Price, cancellationToken);

        var portfolioIds = await dbContext.Positions
            .Where(p =>
                p.Status == PositionStatus.Open &&
                p.Symbol == symbol &&
                p.Exchange == exchange)
            .Select(p => p.Portfolio)
            .Where(p => p.PaperTradeAccountId != null)
            .Select(p => p.PaperTradeAccountId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var accountId in portfolioIds)
        {
            await notifier.NotifyPortfolioUpdatedAsync(accountId, cancellationToken);
        }

        return ServiceResult<MarketQuoteProcessResult>.Success(
            new MarketQuoteProcessResult(filledCount, portfolioIds.Count));
    }

    private async Task UpsertQuoteEntityAsync(
        string symbol,
        string exchange,
        decimal price,
        DateTimeOffset timestamp,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.MarketQuotes
            .FirstOrDefaultAsync(q => q.Symbol == symbol && q.Exchange == exchange, cancellationToken);

        if (entity is null)
        {
            dbContext.MarketQuotes.Add(new MarketQuote
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Exchange = exchange,
                Price = price,
                Timestamp = timestamp,
                UpdatedAt = now
            });
        }
        else
        {
            entity.Price = price;
            entity.Timestamp = timestamp;
            entity.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
