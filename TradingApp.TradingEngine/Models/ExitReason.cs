namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Reason for a risk-based exit signal.
/// </summary>
public enum ExitReason
{
    /// <summary>Price reached the configured stop-loss level.</summary>
    StopLoss = 0,

    /// <summary>Price reached the configured take-profit level.</summary>
    TakeProfit = 1
}
