using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Aggregated outcome of the institutional setup evaluation, including the
/// condition checklist and the resulting trade opportunity when all conditions pass.
/// </summary>
public sealed class InstitutionalSetupResult
{
    /// <summary>
    /// Creates an institutional setup result.
    /// </summary>
    /// <param name="symbol">Traded instrument symbol.</param>
    /// <param name="exchange">Exchange / venue identifier.</param>
    /// <param name="bias">Detected higher-timeframe bias.</param>
    /// <param name="conditions">The six ordered condition checks.</param>
    /// <param name="detectedAt">Evaluation timestamp.</param>
    /// <param name="entryZone">FVG entry zone when the setup is valid.</param>
    /// <param name="entryPrice">Suggested entry price.</param>
    /// <param name="stopLossPrice">Suggested stop-loss price.</param>
    /// <param name="takeProfitPrice">Suggested take-profit price.</param>
    public InstitutionalSetupResult(
        string symbol,
        string exchange,
        MarketBias bias,
        IReadOnlyList<ConditionCheck> conditions,
        DateTimeOffset detectedAt,
        PriceZone? entryZone = null,
        decimal? entryPrice = null,
        decimal? stopLossPrice = null,
        decimal? takeProfitPrice = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentNullException.ThrowIfNull(conditions);

        Symbol = symbol;
        Exchange = exchange;
        Bias = bias;
        Conditions = conditions;
        DetectedAt = detectedAt;
        EntryZone = entryZone;
        EntryPrice = entryPrice;
        StopLossPrice = stopLossPrice;
        TakeProfitPrice = takeProfitPrice;
    }

    /// <summary>Traded instrument symbol.</summary>
    public string Symbol { get; }

    /// <summary>Exchange / venue identifier.</summary>
    public string Exchange { get; }

    /// <summary>Detected higher-timeframe bias.</summary>
    public MarketBias Bias { get; }

    /// <summary>The ordered condition checks.</summary>
    public IReadOnlyList<ConditionCheck> Conditions { get; }

    /// <summary>Evaluation timestamp.</summary>
    public DateTimeOffset DetectedAt { get; }

    /// <summary>FVG entry zone when the setup is valid.</summary>
    public PriceZone? EntryZone { get; }

    /// <summary>Suggested entry price.</summary>
    public decimal? EntryPrice { get; }

    /// <summary>Suggested stop-loss price.</summary>
    public decimal? StopLossPrice { get; }

    /// <summary>Suggested take-profit price.</summary>
    public decimal? TakeProfitPrice { get; }

    /// <summary>True only when every condition passed and a trade opportunity exists.</summary>
    public bool IsSetup => Conditions.Count > 0 && Conditions.All(c => c.Passed) && EntryPrice is not null;

    /// <summary>Fraction of satisfied conditions in the range 0.0 – 1.0.</summary>
    public decimal Confidence =>
        Conditions.Count == 0 ? 0m : (decimal)Conditions.Count(c => c.Passed) / Conditions.Count;
}
