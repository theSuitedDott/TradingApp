using TradingApp.MarketDataFeed;

namespace TradingApp.Extensions;

/// <summary>
/// DI registration for the market data feed infrastructure.
/// </summary>
public static class MarketDataFeedServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configured feed provider and the background worker.
    /// New providers are added here without touching <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddMarketDataFeed(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MarketDataFeedSettings>(
            configuration.GetSection(MarketDataFeedSettings.SectionName));

        var settings = configuration
            .GetSection(MarketDataFeedSettings.SectionName)
            .Get<MarketDataFeedSettings>() ?? new MarketDataFeedSettings();

        switch (settings.Provider)
        {
            case FeedProvider.Simulated:
                services.AddSingleton<IMarketDataFeed, SimulatedMarketDataFeed>();
                break;

            // Future: case FeedProvider.Polygon:
            //     services.AddSingleton<IMarketDataFeed, PolygonWebSocketFeed>();
            //     break;

            default:
                throw new NotSupportedException(
                    $"Market data feed provider '{settings.Provider}' is not registered.");
        }

        services.AddHostedService<MarketDataFeedWorker>();

        return services;
    }
}
