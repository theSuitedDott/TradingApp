using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Provides multi-timeframe candle data for the institutional setup scanner.
/// Implementations may use live market data or deterministic mock data.
/// </summary>
public interface ISetupCandleProvider
{
    /// <summary>
    /// Builds the full multi-timeframe input required by <see cref="IInstitutionalSetupStrategy"/>.
    /// </summary>
    /// <param name="symbol">Instrument symbol (e.g. EUR_USD).</param>
    /// <param name="exchange">Exchange / venue (e.g. OANDA).</param>
    /// <param name="rsiPeriod">RSI period for divergence analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<InstitutionalSetupInput> BuildInputAsync(
        string symbol,
        string exchange,
        int rsiPeriod,
        CancellationToken cancellationToken = default);
}
