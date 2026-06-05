using TradingApp.DTOs.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// In-memory store for detected trade opportunities (demo scope; not persisted).
/// </summary>
public interface ITradeOpportunityStore
{
    /// <summary>Adds a new opportunity, evicting the oldest when the capacity is exceeded.</summary>
    void Add(TradeOpportunityDto opportunity);

    /// <summary>Returns all stored opportunities, newest first.</summary>
    IReadOnlyList<TradeOpportunityDto> GetAll();

    /// <summary>Finds an opportunity by id.</summary>
    TradeOpportunityDto? Find(Guid id);

    /// <summary>Marks an opportunity as executed; returns the updated opportunity or <c>null</c>.</summary>
    TradeOpportunityDto? MarkExecuted(Guid id);

    /// <summary>Returns whether an active opportunity already exists for the symbol and direction.</summary>
    bool HasActive(string symbol, string direction);
}
