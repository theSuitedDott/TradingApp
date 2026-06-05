namespace TradingApp.DTOs.Paper;

/// <summary>
/// Predefined virtual account template for demo onboarding.
/// </summary>
public sealed record DemoAccountPresetDto(
    string Id,
    string Name,
    string Description,
    decimal InitialBalance,
    string BaseCurrency,
    string AccountType);
