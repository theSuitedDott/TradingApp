namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Result of a liquidity sweep (inducement): price ran a prior swing level
/// to grab resting liquidity and then reclaimed it.
/// </summary>
public sealed class SweepResult
{
    /// <summary>
    /// Creates a sweep result.
    /// </summary>
    /// <param name="sweptLevel">The prior swing level whose liquidity was taken.</param>
    /// <param name="extremePrice">The extreme price reached during the sweep (the run-out low/high).</param>
    /// <param name="detail">Human-readable explanation.</param>
    public SweepResult(decimal sweptLevel, decimal extremePrice, string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        if (sweptLevel <= 0 || extremePrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sweptLevel), "Levels must be positive.");
        }

        SweptLevel = sweptLevel;
        ExtremePrice = extremePrice;
        Detail = detail;
    }

    /// <summary>The prior swing level whose liquidity was taken.</summary>
    public decimal SweptLevel { get; }

    /// <summary>The extreme price reached during the sweep (protective level for stops).</summary>
    public decimal ExtremePrice { get; }

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
