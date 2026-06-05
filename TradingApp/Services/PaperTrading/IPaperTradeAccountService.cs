using TradingApp.DTOs.Paper;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Manages virtual paper trade accounts and portfolios.
/// </summary>
public interface IPaperTradeAccountService
{
    /// <summary>Creates a paper account with virtual cash and portfolio.</summary>
    Task<ServiceResult<PaperAccountResponse>> CreateAccountAsync(
        Guid userId,
        CreatePaperAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lists all paper accounts for a user.</summary>
    Task<ServiceResult<IReadOnlyList<PaperAccountResponse>>> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets portfolio summary including PnL for an account.</summary>
    Task<ServiceResult<PortfolioSummaryResponse>> GetPortfolioSummaryAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);
}
