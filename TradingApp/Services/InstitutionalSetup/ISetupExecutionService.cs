using TradingApp.DTOs.Paper;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Executes a detected opportunity as a virtual paper order (one-click trade).
/// </summary>
public interface ISetupExecutionService
{
    /// <summary>
    /// Places a market paper order for the opportunity, carrying its stop-loss and take-profit.
    /// </summary>
    /// <param name="userId">Authenticated user id.</param>
    /// <param name="opportunityId">Opportunity to execute.</param>
    /// <param name="accountId">Target paper account.</param>
    /// <param name="quantity">Order quantity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The placed order, or a failure result.</returns>
    Task<ServiceResult<OrderResponse>> ExecuteAsync(
        Guid userId,
        Guid opportunityId,
        Guid accountId,
        decimal quantity,
        CancellationToken cancellationToken = default);
}
