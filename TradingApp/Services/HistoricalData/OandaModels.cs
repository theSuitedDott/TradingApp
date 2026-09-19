using System.Text.Json.Serialization;

namespace TradingApp.Services.HistoricalData;

internal sealed class OandaCandlesResponse
{
    [JsonPropertyName("instrument")]
    public string? Instrument { get; init; }

    [JsonPropertyName("granularity")]
    public string? Granularity { get; init; }

    [JsonPropertyName("candles")]
    public List<OandaCandle>? Candles { get; init; }
}

internal sealed class OandaCandle
{
    [JsonPropertyName("complete")]
    public bool Complete { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("mid")]
    public OandaOhlc? Mid { get; init; }
}

internal sealed class OandaOhlc
{
    [JsonPropertyName("o")]
    public string? Open { get; init; }

    [JsonPropertyName("h")]
    public string? High { get; init; }

    [JsonPropertyName("l")]
    public string? Low { get; init; }

    [JsonPropertyName("c")]
    public string? Close { get; init; }
}

internal sealed class OandaPricingResponse
{
    [JsonPropertyName("prices")]
    public List<OandaPrice>? Prices { get; init; }
}

internal sealed class OandaPrice
{
    [JsonPropertyName("instrument")]
    public string? Instrument { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("bids")]
    public List<OandaQuoteLevel>? Bids { get; init; }

    [JsonPropertyName("asks")]
    public List<OandaQuoteLevel>? Asks { get; init; }
}

internal sealed class OandaQuoteLevel
{
    [JsonPropertyName("price")]
    public string? Price { get; init; }
}
