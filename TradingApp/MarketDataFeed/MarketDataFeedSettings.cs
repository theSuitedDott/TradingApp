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
    ///   <item><see cref="FeedProvider.Simulated"/> — built-in random-walk generator.</item>
    ///   <item><see cref="FeedProvider.Polygon"/> — Polygon.io WebSocket (future).</item>
    /// </list>
    /// </summary>
    public FeedProvider Provider { get; init; } = FeedProvider.Simulated;

    /// <summary>Instruments to subscribe to.</summary>
    [MinLength(1)]
    public List<SymbolFeedConfig> Symbols { get; init; } = [];

    /// <summary>Milliseconds between ticks (simulated) or reconnect delay.</summary>
    [Range(50, 60_000)]
    public int IntervalMs { get; init; } = 1_000;

    /// <summary>API key for external providers (resolved from environment in production).</summary>
    public string? ApiKey { get; init; }

    /// <summary>WebSocket endpoint for external providers.</summary>
    public string? WebSocketUrl { get; init; }

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

    /// <summary>Polygon.io WebSocket feed (future implementation).</summary>
    Polygon = 1
}
