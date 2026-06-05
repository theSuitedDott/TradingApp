using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Marks open positions to market and updates portfolio total equity.
/// </summary>
public sealed class PortfolioValuationService(
    ApplicationDbContext dbContext,
    IPnlCalculator pnlCalculator) : IPortfolioValuationService
{
    /// <inheritdoc />
    public async Task RecalculateAsync(Guid portfolioId, CancellationToken cancellationToken = default)
    {
        var portfolio = await dbContext.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId, cancellationToken);

        if (portfolio is null)
        {
            return;
        }

        var positionsValue = 0m;
        var now = DateTimeOffset.UtcNow;

        foreach (var position in portfolio.Positions.Where(p => p.Status == PositionStatus.Open))
        {
            if (position.CurrentPrice is null)
            {
                continue;
            }

            position.UnrealizedPnL = pnlCalculator.CalculateUnrealizedLong(
                position.Quantity,
                position.AverageEntryPrice,
                position.CurrentPrice.Value);
            positionsValue += position.CurrentPrice.Value * position.Quantity;
            position.UpdatedAt = now;
        }

        portfolio.TotalEquity = portfolio.CashBalance + positionsValue;
        portfolio.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecalculateByInstrumentAsync(
        string symbol,
        string exchange,
        decimal currentPrice,
        CancellationToken cancellationToken = default)
    {
        var positions = await dbContext.Positions
            .Where(p =>
                p.Status == PositionStatus.Open &&
                p.Symbol == symbol &&
                p.Exchange == exchange)
            .Include(p => p.Portfolio)
            .ToListAsync(cancellationToken);

        if (positions.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var portfolioIds = new HashSet<Guid>();

        foreach (var position in positions)
        {
            position.CurrentPrice = currentPrice;
            position.UnrealizedPnL = pnlCalculator.CalculateUnrealizedLong(
                position.Quantity,
                position.AverageEntryPrice,
                currentPrice);
            position.UpdatedAt = now;
            portfolioIds.Add(position.PortfolioId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var portfolioId in portfolioIds)
        {
            await RecalculateAsync(portfolioId, cancellationToken);
        }
    }
}
