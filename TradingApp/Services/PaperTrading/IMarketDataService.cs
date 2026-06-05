using TradingApp.DTOs.Paper;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Market data ingestion for paper trading simulations.
/// </summary>
public interface IMarketDataService
{
    /// <summary>Submits a quote tick for realtime processing.</summary>
    Task<ServiceResult<MarketQuoteProcessResult>> SubmitQuoteAsync(
        SubmitMarketQuoteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the latest known quote for an instrument.</summary>
    Task<ServiceResult<MarketQuoteResponse>> GetQuoteAsync(
        string symbol,
        string exchange,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Latest market quote response.
/// </summary>
public sealed record MarketQuoteResponse(
    string Symbol,
    string Exchange,
    decimal Price,
    DateTimeOffset Timestamp);
