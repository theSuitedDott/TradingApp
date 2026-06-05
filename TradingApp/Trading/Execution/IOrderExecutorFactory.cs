namespace TradingApp.Trading.Execution;

/// <summary>
/// Resolves the correct order executor for a trading venue.
/// </summary>
public interface IOrderExecutorFactory
{
    /// <summary>
    /// Gets the executor for the requested venue.
    /// </summary>
    /// <param name="venue">Paper or broker.</param>
    /// <returns>Executor implementation.</returns>
    /// <exception cref="NotSupportedException">When the venue is not registered.</exception>
    IOrderExecutor GetExecutor(ExecutionVenue venue);
}
