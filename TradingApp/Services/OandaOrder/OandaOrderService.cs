using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.OandaOrder;

namespace TradingApp.Services.OandaOrder;

/// <summary>
/// Executes live market orders against the OANDA v20 REST API.
/// </summary>
public sealed class OandaOrderService(
    IHttpClientFactory httpClientFactory,
    IOptions<OandaSettings> oandaOptions,
    ILogger<OandaOrderService> logger) : IOandaOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<ServiceResult<OandaTradeDto>> PlaceMarketOrderAsync(
        string instrument,
        decimal units,
        decimal? stopLoss,
        decimal? takeProfit,
        CancellationToken cancellationToken = default)
    {
        var settings = oandaOptions.Value;
        if (!settings.IsConfigured)
        {
            return ServiceResult<OandaTradeDto>.Failure(
                OandaOrderErrorCodes.NotConfigured,
                "OANDA credentials are not configured. Set ApiToken and AccountId via user secrets.");
        }

        var body = new OandaPlaceOrderRequestBody
        {
            Order = new OandaMarketOrderSpec
            {
                Instrument = instrument,
                Units = units.ToString("F0", CultureInfo.InvariantCulture),
                StopLossOnFill = stopLoss.HasValue
                    ? new OandaPriceLevel { Price = stopLoss.Value.ToString("F5", CultureInfo.InvariantCulture) }
                    : null,
                TakeProfitOnFill = takeProfit.HasValue
                    ? new OandaPriceLevel { Price = takeProfit.Value.ToString("F5", CultureInfo.InvariantCulture) }
                    : null
            }
        };

        var json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient("Oanda");
        var url = $"v3/accounts/{settings.AccountId}/orders";

        logger.LogInformation(
            "Placing OANDA market order: {Instrument} {Units} units (SL={SL}, TP={TP}).",
            instrument, units, stopLoss, takeProfit);

        var response = await client.PostAsync(url, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "OANDA order failed ({Status}): {Body}", (int)response.StatusCode, responseBody);
            return ServiceResult<OandaTradeDto>.Failure(
                OandaOrderErrorCodes.ApiError,
                $"OANDA order rejected ({(int)response.StatusCode}): {responseBody}");
        }

        var fill = JsonSerializer.Deserialize<OandaOrderFillResponse>(responseBody, JsonOptions);
        var trade = fill?.OrderFillTransaction?.TradeOpened;

        if (trade?.TradeId is null)
        {
            logger.LogWarning("OANDA response missing tradeOpened. Body: {Body}", responseBody);
            return ServiceResult<OandaTradeDto>.Failure(
                OandaOrderErrorCodes.NoTradeOpened,
                "Order was accepted but no trade was opened (possible margin issue).");
        }

        var openPrice = ParseDecimal(fill!.OrderFillTransaction!.Price) ?? ParseDecimal(trade.Price) ?? 0m;
        var openTime = ParseTime(fill.OrderFillTransaction.Time) ?? DateTimeOffset.UtcNow;
        var filledUnits = ParseDecimal(trade.Units) ?? units;

        var dto = new OandaTradeDto(
            trade.TradeId,
            instrument,
            filledUnits,
            openPrice,
            stopLoss,
            takeProfit,
            openTime,
            0m);

        logger.LogInformation(
            "OANDA order filled: TradeId={TradeId}, Price={Price}, Units={Units}.",
            dto.TradeId, dto.OpenPrice, dto.Units);

        return ServiceResult<OandaTradeDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> CloseTradeAsync(
        string tradeId,
        CancellationToken cancellationToken = default)
    {
        var settings = oandaOptions.Value;
        if (!settings.IsConfigured)
        {
            return ServiceResult<bool>.Failure(
                OandaOrderErrorCodes.NotConfigured,
                "OANDA credentials are not configured.");
        }

        var client = httpClientFactory.CreateClient("Oanda");
        var url = $"v3/accounts/{settings.AccountId}/trades/{tradeId}/close";

        var body = new OandaCloseTradeRequestBody();
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Closing OANDA trade {TradeId}.", tradeId);

        var response = await client.PutAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Close trade failed ({Status}): {Body}", (int)response.StatusCode, responseBody);
            return ServiceResult<bool>.Failure(
                OandaOrderErrorCodes.ApiError,
                $"OANDA close trade failed ({(int)response.StatusCode}): {responseBody}");
        }

        logger.LogInformation("OANDA trade {TradeId} closed.", tradeId);
        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OandaTradeDto>> GetOpenTradesAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = oandaOptions.Value;
        if (!settings.IsConfigured)
        {
            return Array.Empty<OandaTradeDto>();
        }

        var client = httpClientFactory.CreateClient("Oanda");
        var url = $"v3/accounts/{settings.AccountId}/openTrades";

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("GetOpenTrades failed ({Status}): {Body}", (int)response.StatusCode, body);
            return Array.Empty<OandaTradeDto>();
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OandaOpenTradesResponse>(responseBody, JsonOptions);

        if (data?.Trades is null || data.Trades.Count == 0)
        {
            return Array.Empty<OandaTradeDto>();
        }

        var result = new List<OandaTradeDto>(data.Trades.Count);
        foreach (var t in data.Trades)
        {
            if (t.Id is null) continue;
            result.Add(new OandaTradeDto(
                t.Id,
                t.Instrument ?? string.Empty,
                ParseDecimal(t.CurrentUnits) ?? 0m,
                ParseDecimal(t.Price) ?? 0m,
                ParseDecimal(t.StopLossOrder?.Price),
                ParseDecimal(t.TakeProfitOrder?.Price),
                ParseTime(t.OpenTime) ?? DateTimeOffset.UtcNow,
                ParseDecimal(t.UnrealizedPl) ?? 0m));
        }

        return result;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;

    private static DateTimeOffset? ParseTime(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : null;
}
