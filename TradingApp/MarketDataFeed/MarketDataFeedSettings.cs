using System.ComponentModel.DataAnnotations;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Top-level configuration for the automatic market data feed.
/// </summary>
public sealed class MarketDataFeedSettings
{
    public const string SectionName = "MarketDataFeed";

    /// <summary>
    /// Feed provider to use.
    /// <list type="bullet">
    ///   <item><see cref="FeedProvider.Simulated"/> — built-in random-walk generator (offline/demo).</item>
    ///   <item><see cref="FeedProvider.Finnhub"/> — Finnhub REST quote poll, requires <c>Finnhub:ApiKey</c>.</item>
    ///   <item><see cref="FeedProvider.Oanda"/> — OANDA REST pricing poll, requires <c>Oanda:ApiToken</c>.</item>
    /// </list>
    /// </summary>
    public FeedProvider Provider { get; set; } = FeedProvider.Simulated;

    /// <summary>Instruments to subscribe to.</summary>
    [MinLength(1)]
    public List<SymbolFeedConfig> Symbols { get; set; } = [];

    /// <summary>Milliseconds between ticks (simulated) or poll interval for REST providers.</summary>
    [Range(50, 60_000)]
    public int IntervalMs { get; init; } = 5_000;

    /// <summary>Whether the feed should start automatically on application startup.</summary>
    public bool AutoStart { get; init; } = true;
}

/// <summary>
/// Available market data feed providers.
/// </summary>
public enum FeedProvider
{
    /// <summary>Built-in random-walk price simulator (development / demo).</summary>
    Simulated = 0,

    /// <summary>OANDA REST pricing poll for supported forex pairs.</summary>
    Oanda = 2,

    /// <summary>Finnhub REST quote poll for supported forex pairs.</summary>
    Finnhub = 3
}
