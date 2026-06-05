namespace TradingApp.DTOs.Paper;

/// <summary>
/// Virtual portfolio balances and PnL summary.
/// </summary>
public sealed record PortfolioSummaryResponse(
    Guid PortfolioId,
    Guid AccountId,
    decimal CashBalance,
    decimal ReservedCash,
    decimal AvailableCash,
    decimal TotalEquity,
    decimal UnrealizedPnL,
    decimal RealizedPnL,
    string BaseCurrency);
