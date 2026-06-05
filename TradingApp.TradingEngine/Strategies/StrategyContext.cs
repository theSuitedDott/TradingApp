namespace TradingApp.TradingEngine.Strategies;

/// <summary>
/// Immutable context passed to strategies for evaluation.
/// </summary>
public sealed class StrategyContext
{
    /// <summary>
    /// Creates a strategy evaluation context.
    /// </summary>
    /// <param name="market">Current market snapshot.</param>
    /// <param name="openPosition">Optional open long position.</param>
    public StrategyContext(Models.MarketSnapshot market, Models.PositionSnapshot? openPosition = null)
    {
        ArgumentNullException.ThrowIfNull(market);
        Market = market;
        OpenPosition = openPosition;
        HasOpenPosition = openPosition is not null;
    }

    /// <summary>Market data.</summary>
    public Models.MarketSnapshot Market { get; }

    /// <summary>Open position, if any.</summary>
    public Models.PositionSnapshot? OpenPosition { get; }

    /// <summary>Whether a position is currently open.</summary>
    public bool HasOpenPosition { get; }
}
