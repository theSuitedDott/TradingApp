namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Standard long-only PnL formulas.
/// </summary>
public sealed class PnlCalculator : IPnlCalculator
{
    /// <inheritdoc />
    public decimal CalculateUnrealizedLong(decimal quantity, decimal averageEntryPrice, decimal currentPrice) =>
        (currentPrice - averageEntryPrice) * quantity;

    /// <inheritdoc />
    public decimal CalculateRealizedLong(decimal sellQuantity, decimal averageEntryPrice, decimal sellPrice) =>
        (sellPrice - averageEntryPrice) * sellQuantity;
}
