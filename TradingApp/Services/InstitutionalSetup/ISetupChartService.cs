using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Provides OHLC candle data for chart visualization.
/// </summary>
public interface ISetupChartService
{
    /// <summary>
    /// Loads chart candles: Yahoo historical baseline, optional OANDA live tick on the last candle.
    /// </summary>
    /// <param name="symbol">Instrument symbol (OANDA id or Yahoo ticker).</param>
    /// <param name="interval">Candle interval: 1h or 4h.</param>
    /// <param name="range">Fallback lookback when <paramref name="candleCount"/> is null.</param>
    /// <param name="useMock">When true, returns deterministic mock entry-timeframe candles.</param>
    /// <param name="applyLivePrice">When true, merges the latest OANDA tick into the last candle.</param>
    /// <param name="includeTradeLevels">When true, attaches entry/stop/take-profit levels.</param>
    /// <param name="candleCount">Number of historical candles to load from Yahoo (default 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ChartDataDto> GetChartDataAsync(
        string symbol,
        string interval,
        string range,
        bool useMock,
        bool applyLivePrice = false,
        bool includeTradeLevels = false,
        int? candleCount = null,
        CancellationToken cancellationToken = default);
}
