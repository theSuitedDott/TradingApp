namespace TradingApp.Configuration;

/// <summary>
/// Configuration for the Alpha Vantage REST API.
/// Free tier: 25 requests/day, 5 requests/minute.
/// </summary>
public sealed class AlphaVantageSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AlphaVantage";

    /// <summary>
    /// Personal API key from https://www.alphavantage.co/support/#api-key.
    /// Set via user secrets: <c>dotnet user-secrets set "AlphaVantage:ApiKey" "your-key"</c>
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Alpha Vantage REST base URL.</summary>
    public string BaseUrl { get; init; } = "https://www.alphavantage.co/";

    /// <summary>Whether the API key is configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
