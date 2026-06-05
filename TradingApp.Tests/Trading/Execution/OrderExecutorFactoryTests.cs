using TradingApp.Services;
using TradingApp.Services.PaperTrading;
using TradingApp.Trading.Execution;
using Xunit;

namespace TradingApp.Tests.Trading.Execution;

public sealed class OrderExecutorFactoryTests
{
    [Fact]
    public void GetExecutor_ResolvesRegisteredVenues()
    {
        var factory = new OrderExecutorFactory([
            new BrokerOrderExecutor(),
            new PaperOrderExecutor(new NoOpSettlement())
        ]);

        Assert.Equal(ExecutionVenue.Paper, factory.GetExecutor(ExecutionVenue.Paper).Venue);
        Assert.Equal(ExecutionVenue.Broker, factory.GetExecutor(ExecutionVenue.Broker).Venue);
    }

    private sealed class NoOpSettlement : IPortfolioSettlementService
    {
        public Task<ServiceResult<Entities.Order>> ApplyFillAsync(
            Entities.Order order,
            decimal fillPrice,
            decimal fillQuantity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult<Entities.Order>.Success(order));
    }
}
