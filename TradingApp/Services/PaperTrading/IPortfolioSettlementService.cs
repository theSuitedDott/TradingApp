using TradingApp.Entities;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Applies order fills to portfolio cash and positions.
/// </summary>
public interface IPortfolioSettlementService
{
    /// <summary>
    /// Applies a fill to the portfolio linked to the paper order.
    /// </summary>
    Task<ServiceResult<Order>> ApplyFillAsync(
        Order order,
        decimal fillPrice,
        decimal fillQuantity,
        CancellationToken cancellationToken = default);
}
