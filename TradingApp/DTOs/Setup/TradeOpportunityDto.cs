namespace TradingApp.DTOs.Setup;

/// <summary>
/// A ready-to-trade opportunity emitted when every institutional setup condition is satisfied.
/// </summary>
/// <param name="Id">Unique opportunity identifier (used for one-click execution).</param>
/// <param name="Symbol">Instrument symbol.</param>
/// <param name="Exchange">Exchange / venue.</param>
/// <param name="Direction">Trade direction ("Long" or "Short").</param>
/// <param name="Side">Order side ("Buy" or "Sell").</param>
/// <param name="EntryPrice">Suggested entry price (Fair Value Gap).</param>
/// <param name="StopLossPrice">Suggested stop-loss (beyond the swept liquidity).</param>
/// <param name="TakeProfitPrice">Suggested take-profit.</param>
/// <param name="RewardToRisk">Reward-to-risk ratio of the opportunity.</param>
/// <param name="Confidence">Confidence score 0.0 – 1.0.</param>
/// <param name="Message">Localized notification message.</param>
/// <param name="DetectedAt">Detection timestamp.</param>
/// <param name="Status">Opportunity status ("Active" or "Executed").</param>
/// <param name="Conditions">The six condition checks.</param>
public sealed record TradeOpportunityDto(
    Guid Id,
    string Symbol,
    string Exchange,
    string Direction,
    string Side,
    decimal EntryPrice,
    decimal StopLossPrice,
    decimal TakeProfitPrice,
    decimal RewardToRisk,
    decimal Confidence,
    string Message,
    DateTimeOffset DetectedAt,
    string Status,
    IReadOnlyList<ConditionCheckDto> Conditions);
