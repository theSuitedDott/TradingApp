using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Services.HistoricalData;
using Xunit;

namespace TradingApp.Tests.HistoricalData;

public sealed class RoutingHistoricalDataServiceTests
{
    [Fact]
    public async Task GetHistoricalCandlesAsync_RoutesForexToYahooWhenTwelveDataNotConfigured()
    {
        var handler = new RecordingHandler();
        var factory = new NamedClientFactory(handler, "https://query2.finance.yahoo.com/");
        var sut = CreateSut(factory);

        await sut.GetHistoricalCandlesAsync("EUR_USD", "1h", "5d");

        Assert.Contains("finance/chart/EURUSD%3DX", handler.LastRequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHistoricalCandlesAsync_WithCount_RoutesForexToYahoo()
    {
        var handler = new RecordingHandler();
        var factory = new NamedClientFactory(handler, "https://query2.finance.yahoo.com/");
        var sut = CreateSut(factory);

        await sut.GetHistoricalCandlesAsync("EUR_USD", "1h", "60d", candleCount: 1000);

        Assert.Contains("finance/chart/EURUSD%3DX", handler.LastRequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHistoricalCandlesAsync_RoutesIndicesToYahooEndpoint()
    {
        var handler = new RecordingHandler();
        var factory = new NamedClientFactory(handler, "https://query2.finance.yahoo.com/");
        var sut = CreateSut(factory);

        await sut.GetHistoricalCandlesAsync("^VIX", "1d", "60d");

        Assert.Contains("finance/chart/^VIX", handler.LastRequestUri, StringComparison.Ordinal);
    }

    private static RoutingHistoricalDataService CreateSut(NamedClientFactory factory)
    {
        var twelveData = new TwelveDataCandleService(
            factory,
            Options.Create(new TwelveDataSettings()),
            NullLogger<TwelveDataCandleService>.Instance);

        return new RoutingHistoricalDataService(
            twelveData,
            new OandaHistoricalDataService(factory, NullLogger<OandaHistoricalDataService>.Instance),
            new YahooFinanceHistoricalDataService(factory, NullLogger<YahooFinanceHistoricalDataService>.Instance),
            Options.Create(new OandaSettings()),
            NullLogger<RoutingHistoricalDataService>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            const string oandaJson = """{"instrument":"EUR_USD","granularity":"H1","candles":[]}""";
            const string yahooJson = """{"chart":{"result":[{"timestamp":[],"indicators":{"quote":[{"open":[],"high":[],"low":[],"close":[]}]}}]}}""";
            var body = LastRequestUri?.Contains("oanda", StringComparison.OrdinalIgnoreCase) == true
                ? oandaJson
                : yahooJson;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        }
    }

    private sealed class NamedClientFactory(RecordingHandler handler, string baseUrl) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri(baseUrl) };
    }
}
