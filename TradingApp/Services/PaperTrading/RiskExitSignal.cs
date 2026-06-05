namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Risk-based exit triggered for an open position.
/// </summary>
public sealed record RiskExitSignal(
    Guid PositionId,
    Guid PortfolioId,
    Guid PaperTradeAccountId,
    string Symbol,
    string Exchange,
    decimal Quantity,
    decimal TriggerPrice,
    RiskExitReason Reason);

/// <summary>
/// Reason for automatic risk exit.
/// </summary>
public enum RiskExitReason
{
    /// <summary>Stop-loss price reached.</summary>
    StopLoss = 0,

    /// <summary>Take-profit price reached.</summary>
    TakeProfit = 1
}
