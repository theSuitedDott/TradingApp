namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Result of a corrective-exhaustion analysis: three diminishing pushes
/// combined with an RSI divergence against the higher-timeframe trend.
/// </summary>
public sealed class ExhaustionResult
{
    /// <summary>
    /// Creates an exhaustion result.
    /// </summary>
    /// <param name="pushes">The three swing extremes that form the diminishing pushes.</param>
    /// <param name="detail">Human-readable explanation.</param>
    public ExhaustionResult(IReadOnlyList<SwingPoint> pushes, string detail)
    {
        ArgumentNullException.ThrowIfNull(pushes);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        Pushes = pushes;
        Detail = detail;
    }

    /// <summary>The swing extremes (typically three) ending the correction.</summary>
    public IReadOnlyList<SwingPoint> Pushes { get; }

    /// <summary>Final swing extreme that terminates the correction.</summary>
    public SwingPoint LastPush => Pushes[^1];

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
