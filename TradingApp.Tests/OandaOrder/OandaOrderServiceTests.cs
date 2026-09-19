using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Services.OandaOrder;
using Xunit;

namespace TradingApp.Tests.OandaOrder;

public sealed class OandaOrderServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static OandaOrderService BuildSut(
        HttpMessageHandler handler,
        OandaSettings? settings = null)
    {
        var effective = settings ?? new OandaSettings
        {
            ApiToken  = "token123",
            AccountId = "acc456",
            BaseUrl   = "https://api-fxpractice.oanda.com"
        };
        var factory = new StubHttpClientFactory(handler, effective.BaseUrl);
        return new OandaOrderService(
            factory,
            Options.Create(effective),
            NullLogger<OandaOrderService>.Instance);
    }

    // ── PlaceMarketOrderAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task PlaceMarketOrderAsync_ReturnsSuccess_WhenApiRespondsWithTradeOpened()
    {
        const string json = """
            {
              "orderFillTransaction": {
                "price": "1.08500",
                "time": "2024-06-04T10:00:00.000000000Z",
                "tradeOpened": {
                  "tradeID": "42",
                  "units": "1000",
                  "price": "1.08500"
                }
              }
            }
            """;

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json) });

        var sut    = BuildSut(handler);
        var result = await sut.PlaceMarketOrderAsync("EUR_USD", 1000m, 1.07000m, 1.10000m);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("42", result.Value.TradeId);
        Assert.Equal(1.085m, result.Value.OpenPrice);
        Assert.Equal(1000m, result.Value.Units);
        Assert.Equal(1.07000m, result.Value.StopLoss);
        Assert.Equal(1.10000m, result.Value.TakeProfit);
    }

    [Fact]
    public async Task PlaceMarketOrderAsync_ReturnsFailure_WhenApiReturnsError()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errorMessage\":\"STOP_LOSS_ON_FILL_PRICE_PRECISION_EXCEEDED\"}")
            });

        var sut    = BuildSut(handler);
        var result = await sut.PlaceMarketOrderAsync("EUR_USD", 1000m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(OandaOrderErrorCodes.ApiError, result.ErrorCode);
    }

    [Fact]
    public async Task PlaceMarketOrderAsync_ReturnsFailure_WhenCredentialsNotConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(handler, new OandaSettings
        {
            ApiToken  = string.Empty,
            AccountId = string.Empty,
            BaseUrl   = "https://api-fxpractice.oanda.com"
        });

        var result = await sut.PlaceMarketOrderAsync("EUR_USD", 1000m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(OandaOrderErrorCodes.NotConfigured, result.ErrorCode);
    }

    [Fact]
    public async Task PlaceMarketOrderAsync_ReturnsFailure_WhenResponseHasNoTradeOpened()
    {
        const string json = """{ "orderFillTransaction": { "price": "1.08500", "time": "2024-06-04T10:00:00Z" } }""";
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json) });

        var sut    = BuildSut(handler);
        var result = await sut.PlaceMarketOrderAsync("EUR_USD", 1000m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(OandaOrderErrorCodes.NoTradeOpened, result.ErrorCode);
    }

    // ── CloseTradeAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseTradeAsync_ReturnsSuccess_WhenApiAccepts()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var sut    = BuildSut(handler);
        var result = await sut.CloseTradeAsync("42");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CloseTradeAsync_ReturnsFailure_WhenApiReturnsError()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"errorMessage\":\"TRADE_DOESNT_EXIST\"}")
            });

        var sut    = BuildSut(handler);
        var result = await sut.CloseTradeAsync("99999");

        Assert.False(result.IsSuccess);
        Assert.Equal(OandaOrderErrorCodes.ApiError, result.ErrorCode);
    }

    // ── GetOpenTradesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetOpenTradesAsync_ParsesTradesCorrectly()
    {
        const string json = """
            {
              "trades": [
                {
                  "id": "7",
                  "instrument": "EUR_USD",
                  "price": "1.08500",
                  "openTime": "2024-06-04T09:00:00.000000000Z",
                  "currentUnits": "1000",
                  "unrealizedPL": "12.50",
                  "stopLossOrder":   { "price": "1.07500" },
                  "takeProfitOrder": { "price": "1.10000" }
                }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        var sut    = BuildSut(handler);
        var trades = await sut.GetOpenTradesAsync();

        Assert.Single(trades);
        var trade = trades[0];
        Assert.Equal("7", trade.TradeId);
        Assert.Equal("EUR_USD", trade.Instrument);
        Assert.Equal(1000m, trade.Units);
        Assert.Equal(12.50m, trade.UnrealizedPnl);
        Assert.Equal(1.075m, trade.StopLoss);
        Assert.Equal(1.100m, trade.TakeProfit);
    }

    [Fact]
    public async Task GetOpenTradesAsync_ReturnsEmptyList_WhenNotConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(handler, new OandaSettings
        {
            ApiToken  = string.Empty,
            AccountId = string.Empty,
            BaseUrl   = "https://api-fxpractice.oanda.com"
        });

        var trades = await sut.GetOpenTradesAsync();

        Assert.Empty(trades);
    }

    // ── stubs ──────────────────────────────────────────────────────────────────

    private sealed class StubHttpClientFactory(HttpMessageHandler handler, string baseUrl) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
