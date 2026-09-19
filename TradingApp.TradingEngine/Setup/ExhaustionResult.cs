namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Result of a three-push corrective exhaustion: three diminishing counter-trend pushes,
/// entry at the final push and take-profit at the swing extreme before the correction.
/// </summary>
public sealed class ExhaustionResult
{
    /// <summary>
    /// Creates an exhaustion result.
    /// </summary>
    /// <param name="pushes">The three swing extremes that form the diminishing pushes.</param>
    /// <param name="priorPeakPrice">Swing high (long) or low (short) before the correction began.</param>
    /// <param name="detail">Human-readable explanation.</param>
    public ExhaustionResult(
        IReadOnlyList<SwingPoint> pushes,
        decimal priorPeakPrice,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(pushes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(priorPeakPrice, 0m);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        Pushes = pushes;
        PriorPeakPrice = priorPeakPrice;
        Detail = detail;
    }

    /// <summary>The swing extremes (three) ending the correction.</summary>
    public IReadOnlyList<SwingPoint> Pushes { get; }

    /// <summary>Swing extreme before the correction (take-profit target).</summary>
    public decimal PriorPeakPrice { get; }

    /// <summary>Final swing extreme that terminates the correction (entry zone).</summary>
    public SwingPoint LastPush => Pushes[^1];

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
