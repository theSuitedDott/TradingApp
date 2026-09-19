namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Stable identifiers for the six institutional setup conditions, in evaluation order.
/// </summary>
public static class ConditionNames
{
    /// <summary>Clear higher-timeframe (H4) trend.</summary>
    public const string Trend = "H4 Trend";

    /// <summary>Three diminishing counter-trend pushes (entry at 3rd push, target prior peak).</summary>
    public const string Exhaustion = "Exhaustion (3 pushes)";

    /// <summary>Liquidity sweep / inducement on the final push.</summary>
    public const string LiquiditySweep = "Liquidity Sweep (Inducement)";

    /// <summary>Strong displacement candle back in trend direction.</summary>
    public const string Displacement = "Displacement";

    /// <summary>Fair Value Gap entry zone.</summary>
    public const string FairValueGap = "Fair Value Gap";

    /// <summary>DXY and VIX macro confirmation.</summary>
    public const string MacroConfirmation = "DXY/VIX Confirmation";

    /// <summary>All condition names in evaluation order.</summary>
    public static IReadOnlyList<string> Ordered { get; } =
    [
        Trend,
        Exhaustion,
        LiquiditySweep,
        Displacement,
        FairValueGap,
        MacroConfirmation
    ];
}
