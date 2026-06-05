using System.Text.Json.Serialization;

namespace TradingApp.Services.HistoricalData;

public class YahooChartResponse
{
    [JsonPropertyName("chart")]
    public YahooChart? Chart { get; set; }
}

public class YahooChart
{
    [JsonPropertyName("result")]
    public YahooResult[]? Result { get; set; }
    
    [JsonPropertyName("error")]
    public object? Error { get; set; }
}

public class YahooResult
{
    [JsonPropertyName("timestamp")]
    public long[]? Timestamp { get; set; }
    
    [JsonPropertyName("indicators")]
    public YahooIndicators? Indicators { get; set; }
}

public class YahooIndicators
{
    [JsonPropertyName("quote")]
    public YahooQuote[]? Quote { get; set; }
}

public class YahooQuote
{
    [JsonPropertyName("open")]
    public decimal?[]? Open { get; set; }
    
    [JsonPropertyName("high")]
    public decimal?[]? High { get; set; }
    
    [JsonPropertyName("low")]
    public decimal?[]? Low { get; set; }
    
    [JsonPropertyName("close")]
    public decimal?[]? Close { get; set; }
    
    [JsonPropertyName("volume")]
    public long?[]? Volume { get; set; }
}
