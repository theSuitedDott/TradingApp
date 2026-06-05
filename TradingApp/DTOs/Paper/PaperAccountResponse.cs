namespace TradingApp.DTOs.Paper;

/// <summary>
/// Paper trade account summary.
/// </summary>
public sealed record PaperAccountResponse(
    Guid Id,
    string Name,
    string BaseCurrency,
    decimal InitialBalance,
    bool IsDefault,
    string Status,
    DateTimeOffset CreatedAt);
