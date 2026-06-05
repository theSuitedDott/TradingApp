using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Maps historical or mock candles to chart DTOs for the frontend candlestick chart.
/// </summary>
public sealed class SetupChartService(
    IHistoricalDataService historicalDataService,
    IMockSetupCandleProvider mockProvider,
    IInstitutionalSetupScanner scanner,
    ITradeOpportunityStore opportunityStore,
    IMarketQuoteStore quoteStore,
    IOptions<InstitutionalSetupSettings> options) : ISetupChartService
{
    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <inheritdoc />
    public async Task<ChartDataDto> GetChartDataAsync(
        string symbol,
        string interval,
        string range,
        bool useMock,
        bool applyLivePrice = false,
        bool includeTradeLevels = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);
        ArgumentException.ThrowIfNullOrWhiteSpace(range);

        IReadOnlyList<Candle> candles;
        string exchange;
        string source;
        string chartSymbol;

        if (useMock)
        {
            var input = mockProvider.BuildInput(
                _settings.Symbol,
                _settings.Exchange,
                _settings.RsiPeriod);

            candles = interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
                ? CandleAggregator.Aggregate(input.EntryTimeframeCandles, TimeSpan.FromHours(4))
                : input.EntryTimeframeCandles;

            chartSymbol = _settings.Symbol;
            exchange = _settings.Exchange;
            source = "Mock";
        }
        else
        {
            var yahooInterval = interval.Equals("4h", StringComparison.OrdinalIgnoreCase) ? "1h" : interval;
            var lookback = applyLivePrice ? "5d" : range;
            var h1 = await historicalDataService.GetHistoricalCandlesAsync(symbol, yahooInterval, lookback, cancellationToken);

            candles = interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
                ? CandleAggregator.Aggregate(h1, TimeSpan.FromHours(4))
                : h1;

            chartSymbol = symbol;
            exchange = "Yahoo";
            source = applyLivePrice ? "Yahoo (live)" : "Yahoo";
        }

        var dtoCandles = candles.Select(ToDto).ToList();
        decimal? entry = null;
        decimal? stop = null;
        decimal? takeProfit = null;

        if (includeTradeLevels)
        {
            (entry, stop, takeProfit) = ResolveTradeLevels(chartSymbol, exchange, useMock);
        }

        decimal? livePrice = null;
        DateTimeOffset? liveAt = null;
        var isLive = false;

        if (applyLivePrice)
        {
            var quoteSymbol = useMock ? _settings.Symbol : chartSymbol;
            var quoteExchange = useMock ? _settings.Exchange : MapExchangeForQuote(chartSymbol);

            if (quoteStore.TryGetPrice(quoteSymbol, quoteExchange, out var cached))
            {
                liveAt = DateTimeOffset.UtcNow;
                livePrice = cached;
                (dtoCandles, livePrice, liveAt) = ChartLivePriceHelper.Apply(dtoCandles, cached, liveAt.Value);
                isLive = true;
            }
            else if (!useMock && dtoCandles.Count > 0)
            {
                var last = dtoCandles[^1];
                livePrice = last.Close;
                liveAt = last.Time;
                isLive = true;
            }
        }

        return new ChartDataDto(
            chartSymbol,
            exchange,
            interval.ToLowerInvariant(),
            source,
            dtoCandles,
            entry,
            stop,
            takeProfit,
            livePrice,
            liveAt,
            isLive);
    }

    private (decimal? Entry, decimal? Stop, decimal? TakeProfit) ResolveTradeLevels(
        string symbol,
        string exchange,
        bool useMock)
    {
        var stored = opportunityStore.GetAll()
            .Where(o => string.Equals(o.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.DetectedAt)
            .FirstOrDefault();

        if (stored is not null)
        {
            return (stored.EntryPrice, stored.StopLossPrice, stored.TakeProfitPrice);
        }

        if (!useMock)
        {
            return (null, null, null);
        }

        var analysis = scanner.Analyze(symbol, exchange);
        if (analysis.Opportunity is null)
        {
            return (null, null, null);
        }

        return (analysis.Opportunity.EntryPrice, analysis.Opportunity.StopLossPrice, analysis.Opportunity.TakeProfitPrice);
    }

    private static string MapExchangeForQuote(string symbol) =>
        symbol.Contains('=') || symbol.Length > 6 ? "FOREX" : "YAHOO";

    private static ChartCandleDto ToDto(Candle candle) =>
        new(candle.OpenTime, candle.Open, candle.High, candle.Low, candle.Close);
}
