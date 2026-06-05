namespace TradingApp.Services.PaperTrading;

/// <summary>
/// In-memory cache of latest market quotes for fast execution checks.
/// </summary>
public interface IMarketQuoteStore
{
    /// <summary>
    /// Gets the latest cached price if available.
    /// </summary>
    bool TryGetPrice(string symbol, string exchange, out decimal price);

    /// <summary>
    /// Updates the cached price.
    /// </summary>
    void SetPrice(string symbol, string exchange, decimal price, DateTimeOffset timestamp);
}
