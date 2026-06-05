namespace TradingApp.DTOs.Paper;

/// <summary>
/// Virtual order representation.
/// </summary>
public sealed record OrderResponse(
    Guid Id,
    Guid AccountId,
    string Symbol,
    string Exchange,
    string Side,
    string Type,
    string Status,
    decimal Quantity,
    decimal FilledQuantity,
    decimal? LimitPrice,
    decimal? AverageFillPrice,
    decimal Commission,
    string? RejectReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FilledAt);
