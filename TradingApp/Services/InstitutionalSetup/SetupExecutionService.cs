using TradingApp.DTOs.Paper;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Maps a detected opportunity to a paper order and delegates execution to the paper order service.
/// </summary>
public sealed class SetupExecutionService(
    ITradeOpportunityStore opportunityStore,
    IPaperOrderService orderService) : ISetupExecutionService
{
    /// <inheritdoc />
    public async Task<ServiceResult<OrderResponse>> ExecuteAsync(
        Guid userId,
        Guid opportunityId,
        Guid accountId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        var opportunity = opportunityStore.Find(opportunityId);
        if (opportunity is null)
        {
            return ServiceResult<OrderResponse>.Failure(
                SetupErrorCodes.OpportunityNotFound,
                "The requested opportunity no longer exists.");
        }

        if (opportunity.Status != TradeOpportunityStore.ActiveStatus)
        {
            return ServiceResult<OrderResponse>.Failure(
                SetupErrorCodes.OpportunityNotActive,
                "The opportunity has already been executed.");
        }

        var request = new PlacePaperOrderRequest
        {
            Symbol = opportunity.Symbol,
            Exchange = opportunity.Exchange,
            Side = opportunity.Side == "Buy" ? OrderSide.Buy : OrderSide.Sell,
            Type = OrderType.Market,
            Quantity = quantity,
            StopLossPrice = opportunity.StopLossPrice,
            TakeProfitPrice = opportunity.TakeProfitPrice,
            ClientOrderId = $"setup-{opportunity.Id:N}"
        };

        var result = await orderService.PlaceOrderAsync(userId, accountId, request, cancellationToken);
        if (result.IsSuccess)
        {
            opportunityStore.MarkExecuted(opportunity.Id);
        }

        return result;
    }
}
