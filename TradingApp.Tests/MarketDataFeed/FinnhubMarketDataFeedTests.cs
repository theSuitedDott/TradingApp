using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.MarketDataFeed;
using Xunit;

namespace TradingApp.Tests.MarketDataFeed;

public sealed class FinnhubMarketDataFeedTests
{
    private static FinnhubMarketDataFeed BuildSut(
        HttpMessageHandler handler,
        FinnhubSettings? settings = null)
    {
        var effective = settings ?? new FinnhubSettings { ApiKey = "testkey" };
        var factory   = new StubHttpClientFactory(handler, effective.BaseUrl);
        return new FinnhubMarketDataFeed(
            factory,
            Options.Create(effective),
            Options.Create(new MarketDataFeedSettings { IntervalMs = 100 }),
            NullLogger<FinnhubMarketDataFeed>.Instance);
    }

    private static readonly IReadOnlyList<SymbolFeedConfig> EurUsd =
        [new SymbolFeedConfig { Symbol = "EUR_USD", Exchange = "OANDA" }];

    [Fact]
    public async Task StreamAsync_YieldsTick_WhenFinnhubReturnsValidQuote()
    {
        const string json = """{"c":1.08451,"d":0.00054,"dp":0.0499,"h":1.0886,"l":1.0838,"o":1.0840,"pc":1.0840,"t":1704063600}""";

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        var sut = BuildSut(handler);

        using var cts = new CancellationTokenSource();
        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in sut.StreamAsync(EurUsd, cts.Token))
        {
            ticks.Add(tick);
            cts.Cancel();
        }

        Assert.Single(ticks);
        Assert.Equal("EUR_USD", ticks[0].Symbol);
        Assert.Equal("OANDA", ticks[0].Exchange);
        Assert.Equal(1.08451m, ticks[0].Price);
    }

    [Fact]
    public async Task StreamAsync_YieldsNoTicks_WhenApiKeyMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(handler, new FinnhubSettings { ApiKey = string.Empty });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in sut.StreamAsync(EurUsd, cts.Token))
        {
            ticks.Add(tick);
        }

        Assert.Empty(ticks);
    }

    [Fact]
    public async Task StreamAsync_SkipsTick_WhenFinnhubReturnsZeroPrice()
    {
        const string json = """{"c":0,"d":0,"dp":0,"h":0,"l":0,"o":0,"pc":0,"t":0}""";

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        var sut = BuildSut(handler);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in sut.StreamAsync(EurUsd, cts.Token))
        {
            ticks.Add(tick);
        }

        Assert.Empty(ticks);
    }

    [Fact]
    public async Task StreamAsync_SkipsSymbol_WhenNotForexPair()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(handler);

        var nonForex = new List<SymbolFeedConfig>
            { new() { Symbol = "AAPL", Exchange = "NASDAQ" } };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var ticks = new List<TradingApp.Services.PaperTrading.MarketQuoteTick>();

        await foreach (var tick in sut.StreamAsync(nonForex, cts.Token))
        {
            ticks.Add(tick);
        }

        Assert.Empty(ticks);
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
