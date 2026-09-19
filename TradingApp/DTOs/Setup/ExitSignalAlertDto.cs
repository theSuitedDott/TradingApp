namespace TradingApp.DTOs.Setup;

/// <summary>
/// Sell alert when stop-loss or take-profit is reached for an open position.
/// </summary>
/// <param name="PositionId">Open position identifier.</param>
/// <param name="AccountId">Paper account that holds the position.</param>
/// <param name="Symbol">Instrument symbol.</param>
/// <param name="Exchange">Exchange / venue.</param>
/// <param name="Reason">StopLoss or TakeProfit.</param>
/// <param name="MarketPrice">Current market price at scan time.</param>
/// <param name="TriggerPrice">Stop-loss or take-profit level that was hit.</param>
/// <param name="Message">Human-readable alert text.</param>
public sealed record ExitSignalAlertDto(
    Guid PositionId,
    Guid AccountId,
    string Symbol,
    string Exchange,
    string Reason,
    decimal MarketPrice,
    decimal TriggerPrice,
    string Message);
