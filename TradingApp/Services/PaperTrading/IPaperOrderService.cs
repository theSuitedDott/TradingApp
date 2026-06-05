using TradingApp.DTOs.Paper;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Virtual order placement and management for paper trading.
/// </summary>
public interface IPaperOrderService
{
    /// <summary>Places a new virtual order on a paper account.</summary>
    Task<ServiceResult<OrderResponse>> PlaceOrderAsync(
        Guid userId,
        Guid accountId,
        PlacePaperOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending virtual order.</summary>
    Task<ServiceResult<OrderResponse>> CancelOrderAsync(
        Guid userId,
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists orders for a paper account.</summary>
    Task<ServiceResult<IReadOnlyList<OrderResponse>>> GetOrdersAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Lists positions for a paper account portfolio.</summary>
    Task<ServiceResult<IReadOnlyList<PositionResponse>>> GetPositionsAsync(
        Guid userId,
        Guid accountId,
        bool openOnly,
        CancellationToken cancellationToken = default);
}
