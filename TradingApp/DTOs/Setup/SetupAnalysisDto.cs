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
/// <param name="Opportunity">The emitted opportunity, or <c>null</c> when the setup is incomplete.</param>
public sealed record SetupAnalysisDto(
    string Symbol,
    string Exchange,
    string Bias,
    decimal Confidence,
    bool IsSetup,
    IReadOnlyList<ConditionCheckDto> Conditions,
    TradeOpportunityDto? Opportunity);
