using TradingApp.Services.PaperTrading;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Pluggable market data feed that produces quote ticks.
/// Swap this for a real provider (Polygon, Alpha Vantage, etc.) without changing the worker.
/// </summary>
public interface IMarketDataFeed
{
    /// <summary>Name of the provider for logging.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Starts the feed and yields quote ticks until the token is cancelled.
    /// </summary>
    /// <param name="symbols">Instruments to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<MarketQuoteTick> StreamAsync(
        IReadOnlyList<SymbolFeedConfig> symbols,
        CancellationToken cancellationToken);
}
