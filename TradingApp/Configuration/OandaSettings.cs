using System.ComponentModel.DataAnnotations;

namespace TradingApp.Configuration;

/// <summary>
/// Configuration for the OANDA v20 REST API (practice or live).
/// </summary>
public sealed class OandaSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Oanda";

    /// <summary>Personal access token from the OANDA developer portal.</summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>OANDA account id (required for the pricing endpoint).</summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    /// API base URL.
    /// Practice: https://api-fxpractice.oanda.com
    /// Live: https://api-fxtrade.oanda.com
    /// </summary>
    [Required]
    public string BaseUrl { get; init; } = "https://api-fxpractice.oanda.com";

    /// <summary>
    /// If true, every detected 6/6 institutional setup will automatically be placed
    /// as a live market order on OANDA. Requires <see cref="IsConfigured"/> to be true.
    /// </summary>
    public bool LiveOrderEnabled { get; init; } = false;

    /// <summary>
    /// Default trade size in standard lots (1 lot = 100,000 units).
    /// 0.01 = micro lot (1,000 units), 0.1 = mini lot (10,000 units).
    /// </summary>
    public decimal DefaultLots { get; init; } = 0.01m;

    /// <summary>Whether credentials are configured enough to call the API.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(AccountId);
}
