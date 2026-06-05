namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Recommended trading action produced by the engine or a strategy.
/// </summary>
public enum TradingAction
{
    /// <summary>No action recommended.</summary>
    None = 0,

    /// <summary>Open or add to a long position.</summary>
    Buy = 1,

    /// <summary>Close or reduce a long position.</summary>
    Sell = 2
}
