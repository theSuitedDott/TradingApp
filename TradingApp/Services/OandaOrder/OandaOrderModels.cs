using System.Text.Json.Serialization;

namespace TradingApp.Services.OandaOrder;

internal sealed class OandaPlaceOrderRequestBody
{
    [JsonPropertyName("order")]
    public required OandaMarketOrderSpec Order { get; init; }
}

internal sealed class OandaMarketOrderSpec
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "MARKET";

    [JsonPropertyName("instrument")]
    public required string Instrument { get; init; }

    [JsonPropertyName("units")]
    public required string Units { get; init; }

    [JsonPropertyName("stopLossOnFill")]
    public OandaPriceLevel? StopLossOnFill { get; init; }

    [JsonPropertyName("takeProfitOnFill")]
    public OandaPriceLevel? TakeProfitOnFill { get; init; }
}

internal sealed class OandaPriceLevel
{
    [JsonPropertyName("price")]
    public required string Price { get; init; }

    [JsonPropertyName("timeInForce")]
    public string TimeInForce { get; init; } = "GTC";
}

internal sealed class OandaOrderFillResponse
{
    [JsonPropertyName("orderFillTransaction")]
    public OandaFillTransaction? OrderFillTransaction { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }
}

internal sealed class OandaFillTransaction
{
    [JsonPropertyName("tradeOpened")]
    public OandaTradeOpened? TradeOpened { get; init; }

    [JsonPropertyName("price")]
    public string? Price { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }
}

internal sealed class OandaTradeOpened
{
    [JsonPropertyName("tradeID")]
    public string? TradeId { get; init; }

    [JsonPropertyName("units")]
    public string? Units { get; init; }

    [JsonPropertyName("price")]
    public string? Price { get; init; }
}

internal sealed class OandaOpenTradesResponse
{
    [JsonPropertyName("trades")]
    public List<OandaTrade>? Trades { get; init; }
}

internal sealed class OandaTrade
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("instrument")]
    public string? Instrument { get; init; }

    [JsonPropertyName("price")]
    public string? Price { get; init; }

    [JsonPropertyName("openTime")]
    public string? OpenTime { get; init; }

    [JsonPropertyName("currentUnits")]
    public string? CurrentUnits { get; init; }

    [JsonPropertyName("unrealizedPL")]
    public string? UnrealizedPl { get; init; }

    [JsonPropertyName("stopLossOrder")]
    public OandaAttachedOrder? StopLossOrder { get; init; }

    [JsonPropertyName("takeProfitOrder")]
    public OandaAttachedOrder? TakeProfitOrder { get; init; }
}

internal sealed class OandaAttachedOrder
{
    [JsonPropertyName("price")]
    public string? Price { get; init; }
}

internal sealed class OandaCloseTradeRequestBody
{
    [JsonPropertyName("units")]
    public string Units { get; init; } = "ALL";
}
