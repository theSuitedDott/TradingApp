namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Thread-safe in-memory tracker for one-shot sell alerts.
/// </summary>
public sealed class ExitAlertTracker : IExitAlertTracker
{
    private readonly object _gate = new();
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool TryRegister(Guid positionId, string reason)
    {
        var key = $"{positionId:N}:{reason}";
        lock (_gate)
        {
            return _keys.Add(key);
        }
    }

    /// <inheritdoc />
    public void PruneClosedPositions(IReadOnlyCollection<Guid> openPositionIds)
    {
        var open = openPositionIds.ToHashSet();
        lock (_gate)
        {
            _keys.RemoveWhere(k =>
            {
                var idPart = k.Split(':')[0];
                return Guid.TryParse(idPart, out var id) && !open.Contains(id);
            });
        }
    }
}
