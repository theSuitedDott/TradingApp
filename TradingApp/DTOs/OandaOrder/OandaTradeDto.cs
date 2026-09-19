namespace TradingApp.DTOs.OandaOrder;

/// <summary>
/// Represents an open trade on the OANDA live account.
/// </summary>
/// <param name="TradeId">OANDA internal trade identifier.</param>
/// <param name="Instrument">OANDA instrument (e.g. EUR_USD).</param>
/// <param name="Units">Position size in units. Positive = long, negative = short.</param>
/// <param name="OpenPrice">Fill price at which the trade was opened.</param>
/// <param name="StopLoss">Active stop-loss price, or null if not set.</param>
/// <param name="TakeProfit">Active take-profit price, or null if not set.</param>
/// <param name="OpenTime">UTC timestamp when the trade was opened.</param>
/// <param name="UnrealizedPnl">Unrealised profit / loss in account currency.</param>
public sealed record OandaTradeDto(
    string TradeId,
    string Instrument,
    decimal Units,
    decimal OpenPrice,
    decimal? StopLoss,
    decimal? TakeProfit,
    DateTimeOffset OpenTime,
    decimal UnrealizedPnl);
