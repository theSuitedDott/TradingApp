using System.Collections.Concurrent;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Thread-safe in-memory quote cache keyed by symbol and exchange.
/// </summary>
public sealed class MarketQuoteStore : IMarketQuoteStore
{
    private readonly ConcurrentDictionary<string, (decimal Price, DateTimeOffset Timestamp)> _quotes = new();

    /// <inheritdoc />
    public bool TryGetPrice(string symbol, string exchange, out decimal price)
    {
        var key = BuildKey(symbol, exchange);
        if (_quotes.TryGetValue(key, out var entry))
        {
            price = entry.Price;
            return true;
        }

        price = 0;
        return false;
    }

    /// <inheritdoc />
    public void SetPrice(string symbol, string exchange, decimal price, DateTimeOffset timestamp)
    {
        var key = BuildKey(symbol, exchange);
        _quotes[key] = (price, timestamp);
    }

    private static string BuildKey(string symbol, string exchange) =>
        $"{symbol.Trim().ToUpperInvariant()}|{exchange.Trim().ToUpperInvariant()}";
}
