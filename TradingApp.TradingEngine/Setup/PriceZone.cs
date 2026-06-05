namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// A bounded price interval, e.g. a Fair Value Gap or liquidity zone.
/// </summary>
public sealed class PriceZone
{
    /// <summary>
    /// Creates a price zone.
    /// </summary>
    /// <param name="lower">Lower bound (inclusive).</param>
    /// <param name="upper">Upper bound (inclusive).</param>
    public PriceZone(decimal lower, decimal upper)
    {
        if (lower <= 0 || upper <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lower), "Zone bounds must be positive.");
        }

        if (upper < lower)
        {
            throw new ArgumentException("Upper bound must be greater than or equal to lower bound.", nameof(upper));
        }

        Lower = lower;
        Upper = upper;
    }

    /// <summary>Lower bound.</summary>
    public decimal Lower { get; }

    /// <summary>Upper bound.</summary>
    public decimal Upper { get; }

    /// <summary>Midpoint of the zone.</summary>
    public decimal Midpoint => (Lower + Upper) / 2m;

    /// <summary>Returns whether a price falls within the zone (inclusive).</summary>
    /// <param name="price">Price to test.</param>
    public bool Contains(decimal price) => price >= Lower && price <= Upper;
}
