namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Risk-based exit recommendation (stop-loss or take-profit).
/// </summary>
public sealed class ExitSignal
{
    /// <summary>
    /// Creates an exit signal.
    /// </summary>
    /// <param name="action">Typically <see cref="TradingAction.Sell"/> for long positions.</param>
    /// <param name="reason">Exit reason.</param>
    /// <param name="triggerPrice">Price that triggered the exit.</param>
    /// <param name="reasonDetail">Optional detail message.</param>
    public ExitSignal(
        TradingAction action,
        ExitReason reason,
        decimal triggerPrice,
        string? reasonDetail = null)
    {
        if (action == TradingAction.None || action == TradingAction.Buy)
        {
            throw new ArgumentException("Exit signals require a sell action for long positions.", nameof(action));
        }

        if (triggerPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(triggerPrice));
        }

        Action = action;
        Reason = reason;
        TriggerPrice = triggerPrice;
        ReasonDetail = reasonDetail;
    }

    /// <summary>Recommended action to exit.</summary>
    public TradingAction Action { get; }

    /// <summary>Why the exit was triggered.</summary>
    public ExitReason Reason { get; }

    /// <summary>Market price that caused the exit.</summary>
    public decimal TriggerPrice { get; }

    /// <summary>Optional detail text.</summary>
    public string? ReasonDetail { get; }
}
