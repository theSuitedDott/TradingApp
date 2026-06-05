using TradingApp.Entities;
using TradingApp.Services;

namespace TradingApp.Trading.Execution;

/// <summary>
/// Placeholder for live broker execution. Not yet implemented.
/// </summary>
public sealed class BrokerOrderExecutor : IOrderExecutor
{
    /// <inheritdoc />
    public ExecutionVenue Venue => ExecutionVenue.Broker;

    /// <inheritdoc />
    public Task<ServiceResult<Order>> TryExecuteAsync(
        Order order,
        decimal marketPrice,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ServiceResult<Order>.Failure(
            "broker_not_implemented",
            "Live broker execution is not yet available."));
}
