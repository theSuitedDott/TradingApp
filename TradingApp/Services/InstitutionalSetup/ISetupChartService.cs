using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Provides OHLC candle data for chart visualization.
/// </summary>
public interface ISetupChartService
{
    /// <summary>
    /// Loads candles for the chart from Yahoo Finance or mock demo data.
    /// </summary>
    /// <param name="symbol">Instrument symbol (Yahoo ticker or SAP for mock).</param>
    /// <param name="interval">Candle interval: 1h or 4h.</param>
    /// <param name="range">Lookback range for Yahoo (e.g. 60d).</param>
    /// <param name="useMock">When true, returns deterministic mock entry-timeframe candles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ChartDataDto> GetChartDataAsync(
        string symbol,
        string interval,
        string range,
        bool useMock,
        bool applyLivePrice = false,
        bool includeTradeLevels = false,
        CancellationToken cancellationToken = default);
}
