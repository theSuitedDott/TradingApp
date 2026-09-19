using TradingApp.DTOs.OandaOrder;

namespace TradingApp.Services.OandaOrder;

/// <summary>
/// Places and manages live market orders on an OANDA v20 account.
/// </summary>
public interface IOandaOrderService
{
    /// <summary>
    /// Places a market order and returns the resulting open trade.
    /// </summary>
    /// <param name="instrument">OANDA instrument id (e.g. EUR_USD).</param>
    /// <param name="units">
    /// Absolute position size in units (1 lot = 100,000 units).
    /// Pass positive for a buy, negative for a sell.
    /// </param>
    /// <param name="stopLoss">Stop-loss price to attach, or null for none.</param>
    /// <param name="takeProfit">Take-profit price to attach, or null for none.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Services.ServiceResult<OandaTradeDto>> PlaceMarketOrderAsync(
        string instrument,
        decimal units,
        decimal? stopLoss,
        decimal? takeProfit,
        CancellationToken cancellationToken = default);

    /// <summary>Closes an open trade entirely.</summary>
    /// <param name="tradeId">OANDA trade identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Services.ServiceResult<bool>> CloseTradeAsync(
        string tradeId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all currently open trades for the configured account.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OandaTradeDto>> GetOpenTradesAsync(
        CancellationToken cancellationToken = default);
}
