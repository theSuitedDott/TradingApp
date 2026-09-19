namespace TradingApp.Trading.Execution;

/// <summary>
/// Resolves the correct order executor for a trading venue.
/// </summary>
public interface IOrderExecutorFactory
{
    IOrderExecutor GetExecutor(ExecutionVenue venue);
}
