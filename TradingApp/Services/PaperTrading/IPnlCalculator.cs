namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Profit and loss calculations for long positions.
/// </summary>
public interface IPnlCalculator
{
    /// <summary>
    /// Unrealized PnL for an open long position.
    /// </summary>
    decimal CalculateUnrealizedLong(decimal quantity, decimal averageEntryPrice, decimal currentPrice);

    /// <summary>
    /// Realized PnL when selling part or all of a long position.
    /// </summary>
    decimal CalculateRealizedLong(decimal sellQuantity, decimal averageEntryPrice, decimal sellPrice);
}
