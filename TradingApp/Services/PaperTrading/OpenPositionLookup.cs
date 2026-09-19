using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// EF-backed lookup for open paper positions by symbol and exchange.
/// </summary>
public sealed class OpenPositionLookup(ApplicationDbContext dbContext) : IOpenPositionLookup
{
    /// <inheritdoc />
    public Task<bool> HasOpenPositionAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedExchange = exchange.Trim().ToUpperInvariant();

        return dbContext.Positions.AnyAsync(
            p => p.Status == PositionStatus.Open &&
                 p.Symbol == normalizedSymbol &&
                 p.Exchange == normalizedExchange,
            cancellationToken);
    }
}
