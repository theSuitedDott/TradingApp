namespace TradingApp.Trading.Execution;

/// <summary>
/// Target venue for order execution (paper vs. live broker).
/// </summary>
public enum ExecutionVenue
{
    /// <summary>Simulated paper trading.</summary>
    Paper = 0,

    /// <summary>Live broker (future implementation).</summary>
    Broker = 1
}
