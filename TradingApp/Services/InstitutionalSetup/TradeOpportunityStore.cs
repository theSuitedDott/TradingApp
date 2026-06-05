using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Thread-safe, capacity-bounded in-memory store for trade opportunities.
/// </summary>
public sealed class TradeOpportunityStore : ITradeOpportunityStore
{
    /// <summary>Status value for an active (not yet executed) opportunity.</summary>
    public const string ActiveStatus = "Active";

    /// <summary>Status value for an executed opportunity.</summary>
    public const string ExecutedStatus = "Executed";

    private readonly int _capacity;
    private readonly object _gate = new();
    private readonly LinkedList<TradeOpportunityDto> _items = new();

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="capacity">Maximum number of retained opportunities.</param>
    public TradeOpportunityStore(int capacity = 50)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <inheritdoc />
    public void Add(TradeOpportunityDto opportunity)
    {
        ArgumentNullException.ThrowIfNull(opportunity);

        lock (_gate)
        {
            _items.AddFirst(opportunity);
            while (_items.Count > _capacity)
            {
                _items.RemoveLast();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TradeOpportunityDto> GetAll()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    /// <inheritdoc />
    public TradeOpportunityDto? Find(Guid id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(o => o.Id == id);
        }
    }

    /// <inheritdoc />
    public TradeOpportunityDto? MarkExecuted(Guid id)
    {
        lock (_gate)
        {
            var node = _items.First;
            while (node is not null)
            {
                if (node.Value.Id == id)
                {
                    var updated = node.Value with { Status = ExecutedStatus };
                    node.Value = updated;
                    return updated;
                }

                node = node.Next;
            }

            return null;
        }
    }

    /// <inheritdoc />
    public bool HasActive(string symbol, string direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);

        lock (_gate)
        {
            return _items.Any(o =>
                o.Status == ActiveStatus &&
                string.Equals(o.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(o.Direction, direction, StringComparison.OrdinalIgnoreCase));
        }
    }
}
