using TradingApp.Entities;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Long-only stop-loss and take-profit monitor using absolute price levels on positions.
/// </summary>
public sealed class PositionRiskMonitor : IPositionRiskMonitor
{
    /// <inheritdoc />
    public IReadOnlyList<RiskExitSignal> GetTriggeredExits(
        Position position,
        Guid paperTradeAccountId,
        decimal marketPrice)
    {
        if (position.Status != PositionStatus.Open)
        {
            return [];
        }

        var exits = new List<RiskExitSignal>();

        if (position.StopLossPrice is not null && marketPrice <= position.StopLossPrice.Value)
        {
            exits.Add(new RiskExitSignal(
                position.Id,
                position.PortfolioId,
                paperTradeAccountId,
                position.Symbol,
                position.Exchange,
                position.Quantity,
                marketPrice,
                RiskExitReason.StopLoss));
        }
        else if (position.TakeProfitPrice is not null && marketPrice >= position.TakeProfitPrice.Value)
        {
            exits.Add(new RiskExitSignal(
                position.Id,
                position.PortfolioId,
                paperTradeAccountId,
                position.Symbol,
                position.Exchange,
                position.Quantity,
                marketPrice,
                RiskExitReason.TakeProfit));
        }

        return exits;
    }
}
