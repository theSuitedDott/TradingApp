namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Executes automatic risk exits (stop-loss / take-profit) for paper positions.
/// </summary>
public interface IPaperRiskExitService
{
    /// <summary>
    /// Closes a position via a simulated market sell at the trigger price.
    /// </summary>
    Task<ServiceResult<DTOs.Paper.OrderResponse>> ExecuteRiskExitAsync(
        RiskExitSignal exit,
        decimal marketPrice,
        CancellationToken cancellationToken = default);
}
