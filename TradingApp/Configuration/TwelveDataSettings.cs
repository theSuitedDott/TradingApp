namespace TradingApp.Configuration;

/// <summary>
/// Configuration for the Twelve Data REST API.
/// Free tier: 800 API credits/day, 8 credits/minute.
/// Sign up at https://twelvedata.com/
/// </summary>
public sealed class TwelveDataSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "TwelveData";

    /// <summary>
    /// Personal API key from https://twelvedata.com/account/api-keys.
    /// Set via user secrets: <c>dotnet user-secrets set "TwelveData:ApiKey" "your-key"</c>
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Twelve Data REST base URL.</summary>
    public string BaseUrl { get; init; } = "https://api.twelvedata.com/";

    /// <summary>Whether the API key is configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
