namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Input for a single engine evaluation cycle.
/// </summary>
public sealed class TradingEngineRequest
{
    /// <summary>
    /// Creates an engine request.
    /// </summary>
    /// <param name="market">Current market data.</param>
    /// <param name="strategies">Strategies to evaluate (must not be empty).</param>
    /// <param name="openPosition">Optional open long position.</param>
    public TradingEngineRequest(
        MarketSnapshot market,
        IReadOnlyList<Strategies.ITradingStrategy> strategies,
        PositionSnapshot? openPosition = null)
    {
        ArgumentNullException.ThrowIfNull(market);
        ArgumentNullException.ThrowIfNull(strategies);

        if (strategies.Count == 0)
        {
            throw new ArgumentException("At least one strategy is required.", nameof(strategies));
        }

        Market = market;
        Strategies = strategies;
        OpenPosition = openPosition;
    }

    /// <summary>Market snapshot.</summary>
    public MarketSnapshot Market { get; }

    /// <summary>Strategies to run.</summary>
    public IReadOnlyList<Strategies.ITradingStrategy> Strategies { get; }

    /// <summary>Open position, if any.</summary>
    public PositionSnapshot? OpenPosition { get; }
}
