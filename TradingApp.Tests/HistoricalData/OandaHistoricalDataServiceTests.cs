using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TradingApp.Services.HistoricalData;
using Xunit;

namespace TradingApp.Tests.HistoricalData;

public sealed class OandaHistoricalDataServiceTests
{
    [Fact]
    public async Task GetHistoricalCandlesAsync_ParsesOandaResponse()
    {
        const string json = """
            {
              "instrument": "EUR_USD",
              "granularity": "H1",
              "candles": [
                {
                  "complete": true,
                  "time": "2024-06-04T18:00:00.000000000Z",
                  "mid": { "o": "1.08732", "h": "1.08756", "l": "1.08620", "c": "1.08651" }
                }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });

        var factory = new StubHttpClientFactory(handler);
        var sut = new OandaHistoricalDataService(factory, NullLogger<OandaHistoricalDataService>.Instance);

        var candles = await sut.GetHistoricalCandlesAsync("EUR_USD", "1h", "5d");

        Assert.Single(candles);
        Assert.Equal(1.08651m, candles[0].Close);
    }

    [Fact]
    public async Task GetHistoricalCandlesAsync_RejectsUnsupportedSymbol()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var factory = new StubHttpClientFactory(handler);
        var sut = new OandaHistoricalDataService(factory, NullLogger<OandaHistoricalDataService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetHistoricalCandlesAsync("AAPL", "1h", "5d"));
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler) { BaseAddress = new Uri("https://api-fxpractice.oanda.com/") };
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
