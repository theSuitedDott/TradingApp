using Microsoft.Extensions.Options;
using TradingApp.Configuration;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Warns when OANDA is selected but credentials are missing.
/// </summary>
internal sealed class MarketDataFeedOandaFallbackConfigure(
    IOptions<OandaSettings> oandaOptions,
    IConfiguration configuration,
    ILogger<MarketDataFeedOandaFallbackConfigure> logger) : IPostConfigureOptions<MarketDataFeedSettings>
{
    private readonly bool _oandaRequested =
        configuration.GetSection(MarketDataFeedSettings.SectionName).Get<MarketDataFeedSettings>()?.Provider ==
        FeedProvider.Oanda;

    /// <inheritdoc />
    public void PostConfigure(string? name, MarketDataFeedSettings options)
    {
        if (!_oandaRequested || oandaOptions.Value.IsConfigured)
        {
            return;
        }

        logger.LogWarning(
            "OANDA credentials not configured (Oanda:ApiToken / Oanda:AccountId). " +
            "Live ticks are disabled so simulated prices cannot distort the Yahoo chart. " +
            "Set credentials via Rider user secrets, environment variables (Oanda__ApiToken), or appsettings.");
    }
}
