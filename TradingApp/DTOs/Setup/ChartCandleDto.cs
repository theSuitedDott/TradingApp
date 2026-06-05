namespace TradingApp.DTOs.Setup;

/// <summary>
/// OHLC candle for chart rendering.
/// </summary>
/// <param name="Time">Bar open time (UTC).</param>
/// <param name="Open">Open price.</param>
/// <param name="High">High price.</param>
/// <param name="Low">Low price.</param>
/// <param name="Close">Close price.</param>
public sealed record ChartCandleDto(
    DateTimeOffset Time,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close);

/// <summary>
/// Chart payload including optional trade levels for overlays.
/// </summary>
/// <param name="Symbol">Displayed symbol.</param>
/// <param name="Exchange">Data source / venue label.</param>
/// <param name="Interval">Candle interval (e.g. 1h, 4h).</param>
/// <param name="Source">Data source (Yahoo or Mock).</param>
/// <param name="Candles">Ordered candles oldest first.</param>
/// <param name="EntryPrice">Optional entry level for overlay.</param>
/// <param name="StopLossPrice">Optional stop-loss level for overlay.</param>
/// <param name="TakeProfitPrice">Optional take-profit level for overlay.</param>
public sealed record ChartDataDto(
    string Symbol,
    string Exchange,
    string Interval,
    string Source,
    IReadOnlyList<ChartCandleDto> Candles,
    decimal? EntryPrice = null,
    decimal? StopLossPrice = null,
    decimal? TakeProfitPrice = null,
    decimal? LastLivePrice = null,
    DateTimeOffset? LastLivePriceAt = null,
    bool IsLive = false);
