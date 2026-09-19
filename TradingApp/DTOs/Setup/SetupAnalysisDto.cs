namespace TradingApp.DTOs.Setup;

/// <summary>
/// Full institutional setup analysis including the condition checklist and the
/// resulting trade opportunity when all conditions pass.
/// </summary>
/// <param name="Symbol">Instrument symbol.</param>
/// <param name="Exchange">Exchange / venue.</param>
/// <param name="Bias">Detected higher-timeframe bias ("Bullish", "Bearish", "Neutral").</param>
/// <param name="Confidence">Fraction of satisfied conditions (0.0 – 1.0).</param>
/// <param name="IsSetup">True when a valid trade opportunity exists.</param>
/// <param name="Conditions">The six condition checks.</param>
/// <param name="DetectedAt">Evaluation timestamp (UTC) for chart markers and backtest listings.</param>
/// <param name="EntryPrice">3rd-push entry when exhaustion is detected.</param>
/// <param name="StopLossPrice">Stop beyond the sweep or 3rd push.</param>
/// <param name="TakeProfitPrice">Prior swing peak before the correction.</param>
/// <param name="Opportunity">The emitted opportunity, or <c>null</c> when the setup is incomplete.</param>
public sealed record SetupAnalysisDto(
    string Symbol,
    string Exchange,
    string Bias,
    decimal Confidence,
    bool IsSetup,
    IReadOnlyList<ConditionCheckDto> Conditions,
    DateTimeOffset DetectedAt,
    decimal? EntryPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    TradeOpportunityDto? Opportunity);
