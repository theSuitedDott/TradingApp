using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.MarketDataFeed;
using TradingApp.Services.HistoricalData;

namespace TradingApp.Extensions;

/// <summary>
/// DI registration for the market data feed infrastructure.
/// </summary>
public static class MarketDataFeedServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configured feed provider and the background worker.
    /// Supports OANDA, Finnhub, and Simulated providers.
    /// </summary>
    public static IServiceCollection AddMarketDataFeed(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OandaSettings>(configuration.GetSection(OandaSettings.SectionName));

        services.AddOptions<FinnhubSettings>()
            .Bind(configuration.GetSection(FinnhubSettings.SectionName));

        services.AddOptions<AlphaVantageSettings>()
            .Bind(configuration.GetSection(AlphaVantageSettings.SectionName));

        services.AddHttpClient("AlphaVantage", (sp, client) =>
        {
            var av = sp.GetRequiredService<IOptions<AlphaVantageSettings>>().Value;
            client.BaseAddress = new Uri(av.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddOptions<TwelveDataSettings>()
            .Bind(configuration.GetSection(TwelveDataSettings.SectionName));

        services.AddHttpClient("TwelveData", (sp, client) =>
        {
            var td = sp.GetRequiredService<IOptions<TwelveDataSettings>>().Value;
            client.BaseAddress = new Uri(td.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddSingleton<IPostConfigureOptions<MarketDataFeedSettings>, MarketDataFeedOandaFallbackConfigure>();

        services.AddOptions<MarketDataFeedSettings>()
            .Bind(configuration.GetSection(MarketDataFeedSettings.SectionName));

        services.AddHttpClient("Finnhub", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<FinnhubSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddSingleton<SimulatedMarketDataFeed>();
        services.AddSingleton<FinnhubMarketDataFeed>();
        services.AddSingleton<OandaMarketDataFeed>();
        services.AddSingleton<IMarketDataFeed>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MarketDataFeedSettings>>().Value;
            return settings.Provider switch
            {
                FeedProvider.Finnhub   => sp.GetRequiredService<FinnhubMarketDataFeed>(),
                FeedProvider.Oanda     => sp.GetRequiredService<OandaMarketDataFeed>(),
                FeedProvider.Simulated => sp.GetRequiredService<SimulatedMarketDataFeed>(),
                _ => throw new NotSupportedException(
                    $"Market data feed provider '{settings.Provider}' is not supported. " +
                    $"Valid values: Finnhub, Oanda, Simulated.")
            };
        });

        services.AddHostedService<MarketDataFeedWorker>();

        return services;
    }
}
