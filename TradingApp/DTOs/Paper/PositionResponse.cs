namespace TradingApp.DTOs.Paper;

/// <summary>
/// Open or closed virtual position.
/// </summary>
public sealed record PositionResponse(
    Guid Id,
    string Symbol,
    string Exchange,
    string Side,
    string Status,
    decimal Quantity,
    decimal AverageEntryPrice,
    decimal? CurrentPrice,
    decimal UnrealizedPnL,
    decimal RealizedPnL,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
