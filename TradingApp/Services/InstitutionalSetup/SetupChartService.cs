using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.HistoricalData;
using static TradingApp.Services.HistoricalData.ForexSymbolNormalizer;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Chart pipeline: Twelve Data loads forex historical candles (primary, free tier),
/// Yahoo Finance is the fallback for non-forex symbols and when Twelve Data is not configured.
/// Finnhub live ticks update the current candle via <see cref="ChartLivePriceHelper"/>.
/// </summary>
public sealed class SetupChartService(
    TwelveDataCandleService twelveDataCandleService,
    YahooFinanceHistoricalDataService yahooHistoricalDataService,
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
        int? candleCount = null,
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
            var targetCount = candleCount ?? ChartHistoricalSettings.DefaultCandleCount;

            if (IsOandaForex(symbol) && twelveDataCandleService.IsConfigured)
            {
                // Twelve Data: free tier supports forex intraday (800 req/day)
                var oandaSymbol = ToOandaInstrument(symbol);
                var raw = await twelveDataCandleService.GetCandlesAsync(
                    oandaSymbol, interval, targetCount, cancellationToken);

                candles = raw.Count > 0 ? raw : await FallbackToYahoo();

                chartSymbol = oandaSymbol;
                exchange = "OANDA";
                source = applyLivePrice
                    ? (raw.Count > 0 ? "TwelveData · live" : "Yahoo · live")
                    : (raw.Count > 0 ? "TwelveData" : "Yahoo");
            }
            else
            {
                candles = await FallbackToYahoo();

                chartSymbol = IsOandaForex(symbol) ? ToOandaInstrument(symbol) : symbol;
                exchange = IsOandaForex(symbol) ? "OANDA" : "Yahoo";
                source = applyLivePrice ? "Yahoo · live" : "Yahoo";
            }

            async Task<IReadOnlyList<TradingApp.TradingEngine.Models.Candle>> FallbackToYahoo()
            {
                var fetchInterval = interval.Equals("4h", StringComparison.OrdinalIgnoreCase) ? "1h" : interval;
                var yahooSymbol = IsOandaForex(symbol) ? ToYahooSymbol(symbol) : symbol;
                var fetchRange = IsOandaForex(symbol) ? WidenRange(range, minDays: 7) : range;
                var raw = await yahooHistoricalDataService.GetHistoricalCandlesAsync(
                    yahooSymbol, fetchInterval, fetchRange, candleCount: null, cancellationToken);
                return interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
                    ? CandleAggregator.Aggregate(raw, TimeSpan.FromHours(4)).TakeLast(targetCount).ToList()
                    : raw.TakeLast(targetCount).ToList();
            }
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
            var quoteExchange = useMock ? _settings.Exchange : MapExchangeForLiveQuote(chartSymbol);

            if (quoteStore.TryGetPrice(quoteSymbol, quoteExchange, out var cached))
            {
                var lastClose = dtoCandles.Count > 0 ? dtoCandles[^1].Close : cached;
                var deviation = Math.Abs(cached - lastClose) / Math.Max(Math.Abs(lastClose), 0.000001m);
                if (deviation <= 0.02m)
                {
                    liveAt = DateTimeOffset.UtcNow;
                    var liveResult = ChartLivePriceHelper.Apply(dtoCandles, cached, liveAt.Value);
                    dtoCandles = liveResult.Candles.ToList();
                    livePrice = liveResult.LivePrice;
                    liveAt = liveResult.LiveAt;
                    isLive = true;
                }
            }
            else if (!useMock && dtoCandles.Count > 0)
            {
                var last = dtoCandles[^1];
                livePrice = last.Close;
                liveAt = last.Time;
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

    private static string MapExchangeForLiveQuote(string symbol) =>
        IsOandaForex(symbol) ? "OANDA" : "YAHOO";

    /// <summary>
    /// Ensures the range covers at least <paramref name="minDays"/> days so weekends
    /// and Yahoo data gaps don't cut off the most recent trading day.
    /// </summary>
    private static string WidenRange(string range, int minDays = 5)
    {
        if (range.EndsWith('d') && int.TryParse(range[..^1], out var days) && days < minDays)
            return $"{minDays}d";
        return range;
    }

    private static ChartCandleDto ToDto(Candle candle) =>
        new(candle.OpenTime, candle.Open, candle.High, candle.Low, candle.Close);
}
