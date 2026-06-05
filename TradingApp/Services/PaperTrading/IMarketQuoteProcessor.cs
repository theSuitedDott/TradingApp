namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Processes realtime market quotes: persists price, fills limits, updates PnL.
/// </summary>
public interface IMarketQuoteProcessor
{
    /// <summary>
    /// Ingests a quote tick and runs paper trading side effects.
    /// </summary>
    Task<ServiceResult<MarketQuoteProcessResult>> ProcessQuoteAsync(
        MarketQuoteTick tick,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Incoming market quote tick.
/// </summary>
public sealed record MarketQuoteTick(
    string Symbol,
    string Exchange,
    decimal Price,
    DateTimeOffset Timestamp);

/// <summary>
/// Result of quote processing.
/// </summary>
public sealed record MarketQuoteProcessResult(
    int FilledOrdersCount,
    int UpdatedPortfoliosCount);
