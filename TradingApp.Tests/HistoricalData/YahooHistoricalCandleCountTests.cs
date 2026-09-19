using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TradingApp.Services.HistoricalData;
using Xunit;

namespace TradingApp.Tests.HistoricalData;

public sealed class YahooHistoricalCandleCountTests
{
    [Fact]
    public async Task GetHistoricalCandlesAsync_WithCount_UsesPeriodParameters()
    {
        string? requestUri = null;
        var handler = new StubHandler(req =>
        {
            requestUri = req.RequestUri?.ToString();
            const string json = """
                {
                  "chart": {
                    "result": [{
                      "timestamp": [1717531200],
                      "indicators": { "quote": [{ "open": [1.08], "high": [1.09], "low": [1.07], "close": [1.085] }] }
                    }]
                  }
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });

        var sut = new YahooFinanceHistoricalDataService(
            new StubFactory(handler),
            NullLogger<YahooFinanceHistoricalDataService>.Instance);

        await sut.GetHistoricalCandlesAsync("EURUSD=X", "1h", "60d", candleCount: 1000);

        Assert.NotNull(requestUri);
        Assert.Contains("period1=", requestUri, StringComparison.Ordinal);
        Assert.Contains("period2=", requestUri, StringComparison.Ordinal);
        Assert.DoesNotContain("range=", requestUri, StringComparison.Ordinal);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class StubFactory(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
