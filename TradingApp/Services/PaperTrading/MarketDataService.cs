using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Paper;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Facade for market quote submission and retrieval.
/// </summary>
public sealed class MarketDataService(
    ApplicationDbContext dbContext,
    IMarketQuoteProcessor quoteProcessor,
    IMarketQuoteStore quoteStore) : IMarketDataService
{
    /// <inheritdoc />
    public Task<ServiceResult<MarketQuoteProcessResult>> SubmitQuoteAsync(
        SubmitMarketQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var tick = new MarketQuoteTick(
            request.Symbol.Trim().ToUpperInvariant(),
            request.Exchange.Trim().ToUpperInvariant(),
            request.Price,
            request.Timestamp ?? DateTimeOffset.UtcNow);

        return quoteProcessor.ProcessQuoteAsync(tick, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<MarketQuoteResponse>> GetQuoteAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedExchange = exchange.Trim().ToUpperInvariant();

        if (quoteStore.TryGetPrice(normalizedSymbol, normalizedExchange, out var cached))
        {
            return ServiceResult<MarketQuoteResponse>.Success(
                new MarketQuoteResponse(normalizedSymbol, normalizedExchange, cached, DateTimeOffset.UtcNow));
        }

        var entity = await dbContext.MarketQuotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                q => q.Symbol == normalizedSymbol && q.Exchange == normalizedExchange,
                cancellationToken);

        if (entity is null)
        {
            return ServiceResult<MarketQuoteResponse>.Failure(
                PaperTradingErrorCodes.NoMarketPrice,
                "No quote available for instrument.");
        }

        return ServiceResult<MarketQuoteResponse>.Success(
            new MarketQuoteResponse(entity.Symbol, entity.Exchange, entity.Price, entity.Timestamp));
    }
}
