namespace TradingApp.DTOs.Paper;

/// <summary>
/// Linked broker account summary (demo or live-prepared).
/// </summary>
public sealed record BrokerAccountResponse(
    Guid Id,
    string Name,
    string BrokerName,
    string? ExternalAccountId,
    string BaseCurrency,
    bool IsLiveTradingEnabled,
    string Status,
    string ConnectionMode,
    DateTimeOffset CreatedAt);
