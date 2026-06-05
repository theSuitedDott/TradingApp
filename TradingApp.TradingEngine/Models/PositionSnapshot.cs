namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Open position state used for sell signals and risk evaluation.
/// </summary>
public sealed class PositionSnapshot
{
    /// <summary>
    /// Initializes a long position snapshot.
    /// </summary>
    /// <param name="quantity">Held quantity (must be positive).</param>
    /// <param name="averageEntryPrice">Volume-weighted entry price.</param>
    /// <param name="stopLossPrice">Optional absolute stop-loss price.</param>
    /// <param name="takeProfitPrice">Optional absolute take-profit price.</param>
    public PositionSnapshot(
        decimal quantity,
        decimal averageEntryPrice,
        decimal? stopLossPrice = null,
        decimal? takeProfitPrice = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        }

        if (averageEntryPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averageEntryPrice), averageEntryPrice,
                "Entry price must be positive.");
        }

        Quantity = quantity;
        AverageEntryPrice = averageEntryPrice;
        StopLossPrice = stopLossPrice;
        TakeProfitPrice = takeProfitPrice;
    }

    /// <summary>Held quantity.</summary>
    public decimal Quantity { get; }

    /// <summary>Average entry price.</summary>
    public decimal AverageEntryPrice { get; }

    /// <summary>Absolute stop-loss trigger price (long).</summary>
    public decimal? StopLossPrice { get; }

    /// <summary>Absolute take-profit trigger price (long).</summary>
    public decimal? TakeProfitPrice { get; }
}
