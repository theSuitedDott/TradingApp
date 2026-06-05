using TradingApp.DTOs.Paper;
using TradingApp.DTOs.Setup;
using TradingApp.Entities.Enums;
using TradingApp.Services;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class SetupExecutionServiceTests
{
    private static TradeOpportunityDto LongOpportunity() =>
        new(
            Guid.NewGuid(), "SAP", "XETRA", "Long", "Buy",
            109.75m, 101.9m, 125.45m, 2m, 1m, "msg", DateTimeOffset.UtcNow,
            TradeOpportunityStore.ActiveStatus, []);

    [Fact]
    public async Task ExecuteAsync_PlacesOrderAndMarksExecuted()
    {
        var store = new TradeOpportunityStore();
        var opp = LongOpportunity();
        store.Add(opp);
        var orderService = new CapturingOrderService();
        var sut = new SetupExecutionService(store, orderService);

        var result = await sut.ExecuteAsync(Guid.NewGuid(), opp.Id, Guid.NewGuid(), quantity: 10m);

        Assert.True(result.IsSuccess);
        Assert.NotNull(orderService.LastRequest);
        Assert.Equal(OrderSide.Buy, orderService.LastRequest!.Side);
        Assert.Equal(OrderType.Market, orderService.LastRequest.Type);
        Assert.Equal(opp.StopLossPrice, orderService.LastRequest.StopLossPrice);
        Assert.Equal(opp.TakeProfitPrice, orderService.LastRequest.TakeProfitPrice);
        Assert.Equal(TradeOpportunityStore.ExecutedStatus, store.Find(opp.Id)!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpportunityMissing_ReturnsNotFound()
    {
        var sut = new SetupExecutionService(new TradeOpportunityStore(), new CapturingOrderService());

        var result = await sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetupErrorCodes.OpportunityNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyExecuted_ReturnsConflict()
    {
        var store = new TradeOpportunityStore();
        var opp = LongOpportunity();
        store.Add(opp);
        store.MarkExecuted(opp.Id);
        var sut = new SetupExecutionService(store, new CapturingOrderService());

        var result = await sut.ExecuteAsync(Guid.NewGuid(), opp.Id, Guid.NewGuid(), 1m);

        Assert.False(result.IsSuccess);
        Assert.Equal(SetupErrorCodes.OpportunityNotActive, result.ErrorCode);
    }

    private sealed class CapturingOrderService : IPaperOrderService
    {
        public PlacePaperOrderRequest? LastRequest { get; private set; }

        public Task<ServiceResult<OrderResponse>> PlaceOrderAsync(
            Guid userId, Guid accountId, PlacePaperOrderRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            var response = new OrderResponse(
                Guid.NewGuid(), accountId, request.Symbol, request.Exchange,
                request.Side.ToString(), request.Type.ToString(), "Filled",
                request.Quantity, request.Quantity, request.LimitPrice, 109.75m, 1m, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            return Task.FromResult(ServiceResult<OrderResponse>.Success(response));
        }

        public Task<ServiceResult<OrderResponse>> CancelOrderAsync(
            Guid userId, Guid accountId, Guid orderId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ServiceResult<IReadOnlyList<OrderResponse>>> GetOrdersAsync(
            Guid userId, Guid accountId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ServiceResult<IReadOnlyList<PositionResponse>>> GetPositionsAsync(
            Guid userId, Guid accountId, bool openOnly, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
