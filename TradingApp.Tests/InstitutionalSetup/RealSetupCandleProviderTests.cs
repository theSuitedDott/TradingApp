using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.InstitutionalSetup;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

public sealed class RealSetupCandleProviderTests
{
    private const string YahooJson = """
        {
          "chart": {
            "result": [{
              "timestamp": [1717531200, 1717534800, 1717538400, 1717542000, 1717545600,
                            1717549200, 1717552800, 1717556400, 1717560000, 1717563600],
              "indicators": { "quote": [{ "open":  [1.08,1.09,1.08,1.07,1.08,1.09,1.08,1.07,1.08,1.09],
                                          "high":  [1.09,1.10,1.09,1.08,1.09,1.10,1.09,1.08,1.09,1.10],
                                          "low":   [1.07,1.08,1.07,1.06,1.07,1.08,1.07,1.06,1.07,1.08],
                                          "close": [1.085,1.095,1.085,1.075,1.085,1.095,1.085,1.075,1.085,1.095] }] }
            }]
          }
        }
        """;

    [Fact]
    public async Task BuildInputAsync_ReturnsInputWithAllTimeframes_WhenYahooResponds()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(YahooJson) });
        var factory = new StubHttpClientFactory(handler);
        var yahoo   = new YahooFinanceHistoricalDataService(factory, NullLogger<YahooFinanceHistoricalDataService>.Instance);
        var sut     = new RealSetupCandleProvider(yahoo, NullLogger<RealSetupCandleProvider>.Instance);

        var input = await sut.BuildInputAsync("EUR_USD", "OANDA", 3);

        Assert.NotNull(input);
        Assert.Equal("EUR_USD", input.Symbol);
        Assert.Equal("OANDA", input.Exchange);
        Assert.NotEmpty(input.HigherTimeframeCandles);
        Assert.NotEmpty(input.DxyCandles);
        Assert.NotEmpty(input.VixCandles);
        Assert.Equal(3, input.RsiPeriod);
    }

    [Fact]
    public async Task BuildInputAsync_ConvertsForexSymbolToYahoo()
    {
        var requestedSymbols = new List<string>();

        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            // Extract symbol from Yahoo Finance URL
            var start = uri.IndexOf("/chart/", StringComparison.Ordinal) + 7;
            var end   = uri.IndexOf("?", start, StringComparison.Ordinal);
            if (start > 7 && end > start)
            {
                requestedSymbols.Add(Uri.UnescapeDataString(uri[start..end]));
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(YahooJson) };
        });

        var factory = new StubHttpClientFactory(handler);
        var yahoo   = new YahooFinanceHistoricalDataService(factory, NullLogger<YahooFinanceHistoricalDataService>.Instance);
        var sut     = new RealSetupCandleProvider(yahoo, NullLogger<RealSetupCandleProvider>.Instance);

        await sut.BuildInputAsync("EUR_USD", "OANDA", 3);

        // EUR_USD must be converted to the Yahoo ticker EURUSD=X
        Assert.Contains("EURUSD=X", requestedSymbols);
        // DXY and VIX must also be fetched
        Assert.Contains("DX-Y.NYB", requestedSymbols);
        Assert.Contains("^VIX", requestedSymbols);
    }

    [Fact]
    public async Task BuildInputAsync_ReturnsEmptyCandles_WhenYahooFails()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var factory = new StubHttpClientFactory(handler);
        var yahoo   = new YahooFinanceHistoricalDataService(factory, NullLogger<YahooFinanceHistoricalDataService>.Instance);
        var sut     = new RealSetupCandleProvider(yahoo, NullLogger<RealSetupCandleProvider>.Instance);

        // Should not throw — returns empty candle lists gracefully.
        var input = await sut.BuildInputAsync("EUR_USD", "OANDA", 3);

        Assert.NotNull(input);
        Assert.Empty(input.HigherTimeframeCandles);
        Assert.Empty(input.DxyCandles);
        Assert.Empty(input.VixCandles);
    }

    // ── stubs ──────────────────────────────────────────────────────────────────

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
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
