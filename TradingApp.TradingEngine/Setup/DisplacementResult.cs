using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Result of displacement detection: a strong, expanding candle that drives
/// price back in the trend direction.
/// </summary>
public sealed class DisplacementResult
{
    /// <summary>
    /// Creates a displacement result.
    /// </summary>
    /// <param name="index">Index of the displacement candle in the source series.</param>
    /// <param name="candle">The displacement candle.</param>
    /// <param name="bodyRatio">Body size relative to the recent average body.</param>
    /// <param name="detail">Human-readable explanation.</param>
    public DisplacementResult(int index, Candle candle, decimal bodyRatio, string detail)
    {
        ArgumentNullException.ThrowIfNull(candle);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Index = index;
        Candle = candle;
        BodyRatio = bodyRatio;
        Detail = detail;
    }

    /// <summary>Index of the displacement candle in the source series.</summary>
    public int Index { get; }

    /// <summary>The displacement candle.</summary>
    public Candle Candle { get; }

    /// <summary>Body size relative to the recent average body.</summary>
    public decimal BodyRatio { get; }

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
