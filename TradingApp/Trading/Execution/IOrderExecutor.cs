using TradingApp.Entities;
using TradingApp.Services;

namespace TradingApp.Trading.Execution;

/// <summary>
/// Broker-agnostic order execution port. Paper and live brokers implement this contract.
/// </summary>
public interface IOrderExecutor
{
    /// <summary>Venue handled by this executor.</summary>
    ExecutionVenue Venue { get; }

    /// <summary>
    /// Attempts to fill or advance a pending order at the given market price.
    /// </summary>
    /// <param name="order">Order to execute.</param>
    /// <param name="marketPrice">Current market price.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result when a fill occurs or order is rejected.</returns>
    Task<ServiceResult<Order>> TryExecuteAsync(
        Order order,
        decimal marketPrice,
        CancellationToken cancellationToken = default);
}
