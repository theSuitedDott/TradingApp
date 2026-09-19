namespace TradingApp.Configuration;

/// <summary>
/// Configuration for the Finnhub REST API.
/// </summary>
public sealed class FinnhubSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Finnhub";

    /// <summary>
    /// Personal API key from https://finnhub.io/dashboard.
    /// Set via user secrets: <c>dotnet user-secrets set "Finnhub:ApiKey" "your-key"</c>
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Finnhub REST base URL.</summary>
    public string BaseUrl { get; init; } = "https://finnhub.io/api/v1/";

    /// <summary>Whether the API key is configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
