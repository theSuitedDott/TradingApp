namespace TradingApp.TradingEngine.Risk;

/// <summary>
/// Context for stop-loss and take-profit evaluation.
/// </summary>
public sealed class PositionRiskContext
{
    /// <summary>
    /// Creates a risk evaluation context.
    /// </summary>
    /// <param name="market">Current market snapshot.</param>
    /// <param name="openPosition">Open long position with risk levels.</param>
    public PositionRiskContext(Models.MarketSnapshot market, Models.PositionSnapshot openPosition)
    {
        ArgumentNullException.ThrowIfNull(market);
        ArgumentNullException.ThrowIfNull(openPosition);
        Market = market;
        OpenPosition = openPosition;
    }

    /// <summary>Current market data.</summary>
    public Models.MarketSnapshot Market { get; }

    /// <summary>Open position including stop-loss and take-profit prices.</summary>
    public Models.PositionSnapshot OpenPosition { get; }
}
