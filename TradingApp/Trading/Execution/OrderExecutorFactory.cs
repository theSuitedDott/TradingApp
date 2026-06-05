namespace TradingApp.Trading.Execution;

/// <summary>
/// Default factory that resolves executors from DI.
/// </summary>
public sealed class OrderExecutorFactory(IEnumerable<IOrderExecutor> executors) : IOrderExecutorFactory
{
    private readonly IReadOnlyDictionary<ExecutionVenue, IOrderExecutor> _executors =
        executors.ToDictionary(e => e.Venue);

    /// <inheritdoc />
    public IOrderExecutor GetExecutor(ExecutionVenue venue)
    {
        if (_executors.TryGetValue(venue, out var executor))
        {
            return executor;
        }

        throw new NotSupportedException($"No order executor registered for venue '{venue}'.");
    }
}
