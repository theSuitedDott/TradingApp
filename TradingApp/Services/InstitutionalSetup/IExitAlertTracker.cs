namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Prevents duplicate sell alerts for the same position and exit reason.
/// </summary>
public interface IExitAlertTracker
{
    /// <summary>
    /// Returns true the first time an alert for this position and reason is recorded.
    /// </summary>
    bool TryRegister(Guid positionId, string reason);

    /// <summary>Removes tracking entries for positions that are no longer open.</summary>
    void PruneClosedPositions(IReadOnlyCollection<Guid> openPositionIds);
}
