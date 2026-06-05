using TradingApp.Entities;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Detects stop-loss and take-profit triggers on open positions.
/// </summary>
public interface IPositionRiskMonitor
{
    /// <summary>
    /// Finds positions that should be closed at the given market price.
    /// </summary>
    IReadOnlyList<RiskExitSignal> GetTriggeredExits(
        Entities.Position position,
        Guid paperTradeAccountId,
        decimal marketPrice);
}
