using System.Runtime.CompilerServices;
using TradingApp.Services.PaperTrading;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Deterministic random-walk price simulator.
/// Each tick moves the price by a random percentage within ±<see cref="SymbolFeedConfig.SimulatedVolatilityPercent"/>.
/// Intended for development, demos, and backtesting harnesses — not for production feeds.
/// </summary>
public sealed class SimulatedMarketDataFeed(
    ILogger<SimulatedMarketDataFeed> logger) : IMarketDataFeed
{
    /// <inheritdoc />
    public string ProviderName => "Simulated";

    /// <inheritdoc />
    public async IAsyncEnumerable<MarketQuoteTick> StreamAsync(
        IReadOnlyList<SymbolFeedConfig> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            logger.LogWarning("SimulatedMarketDataFeed started with no symbols configured.");
            yield break;
        }

        var prices = symbols.ToDictionary(
            s => BuildKey(s.Symbol, s.Exchange),
            s => s.SimulatedBasePrice);

        logger.LogInformation(
            "SimulatedMarketDataFeed streaming {Count} symbol(s): {Symbols}",
            symbols.Count,
            string.Join(", ", symbols.Select(s => $"{s.Symbol}.{s.Exchange}")));

        var random = new Random();
        var index = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var cfg = symbols[index % symbols.Count];
            var key = BuildKey(cfg.Symbol, cfg.Exchange);

            prices[key] = ComputeNextPrice(prices[key], cfg.SimulatedVolatilityPercent, random);

            var tick = new MarketQuoteTick(
                cfg.Symbol.ToUpperInvariant(),
                cfg.Exchange.ToUpperInvariant(),
                prices[key],
                DateTimeOffset.UtcNow);

            logger.LogDebug(
                "Simulated tick {Symbol}.{Exchange} @ {Price:F4}",
                tick.Symbol, tick.Exchange, tick.Price);

            yield return tick;

            index++;

            // Delay only after a full round-trip through all symbols
            if (index % symbols.Count == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(0), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Advances price by a random walk step clamped to the volatility band.
    /// Uses Geometric Brownian Motion (multiplicative) to keep prices positive.
    /// </summary>
    private static decimal ComputeNextPrice(decimal currentPrice, decimal volatilityPercent, Random random)
    {
        var changePercent = ((decimal)random.NextDouble() * 2m - 1m) * volatilityPercent;
        var newPrice = currentPrice * (1m + changePercent);
        return Math.Max(0.01m, Math.Round(newPrice, 4));
    }

    private static string BuildKey(string symbol, string exchange) =>
        $"{symbol.ToUpperInvariant()}|{exchange.ToUpperInvariant()}";
}
