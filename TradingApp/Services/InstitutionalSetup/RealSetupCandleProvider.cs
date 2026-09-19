using TradingApp.Services.HistoricalData;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Fetches live multi-timeframe candle data from Yahoo Finance for the institutional setup scanner.
/// <list type="bullet">
///   <item>Higher timeframe (H4): H1 candles aggregated to 4-hour bars over 60 days.</item>
///   <item>Entry timeframe (H1): last 14 days of 1-hour candles.</item>
///   <item>DXY (macro): 60 days of daily Dollar Index candles (<c>DX-Y.NYB</c>).</item>
///   <item>VIX (macro): 60 days of daily volatility index candles (<c>^VIX</c>).</item>
/// </list>
/// </summary>
public sealed class RealSetupCandleProvider(
    YahooFinanceHistoricalDataService yahooService,
    ILogger<RealSetupCandleProvider> logger) : ISetupCandleProvider
{
    private const string DxySymbol = "DX-Y.NYB";
    private const string VixSymbol = "^VIX";

    /// <summary>Days of H1 history to fetch for H4 aggregation.</summary>
    private const string H4Range = "60d";

    /// <summary>Days of H1 history for the entry timeframe.</summary>
    private const int H1EntryDays = 14;

    /// <summary>Days of daily history for DXY and VIX.</summary>
    private const string MacroRange = "60d";

    /// <inheritdoc />
    public async Task<InstitutionalSetupInput> BuildInputAsync(
        string symbol,
        string exchange,
        int rsiPeriod,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);

        var yahooSymbol = ForexSymbolNormalizer.IsOandaForex(symbol)
            ? ForexSymbolNormalizer.ToYahooSymbol(symbol)
            : symbol;

        logger.LogDebug(
            "Fetching real candle data for setup scan: {Symbol} ({Yahoo}), DXY, VIX.",
            symbol, yahooSymbol);

        // Fetch all three data sources in parallel to minimize latency.
        var h1Task  = FetchSafeAsync(yahooSymbol, "1h", H4Range, cancellationToken);
        var dxyTask = FetchSafeAsync(DxySymbol, "1d", MacroRange, cancellationToken);
        var vixTask = FetchSafeAsync(VixSymbol, "1d", MacroRange, cancellationToken);

        await Task.WhenAll(h1Task, dxyTask, vixTask).ConfigureAwait(false);

        var h1Raw  = h1Task.Result;
        var dxy    = dxyTask.Result;
        var vix    = vixTask.Result;

        // Aggregate H1 → H4 for the higher timeframe trend filter.
        var h4Candles = h1Raw.Count > 0
            ? CandleAggregator.Aggregate(h1Raw, TimeSpan.FromHours(4))
            : Array.Empty<Candle>();

        // Entry timeframe: last 14 days of H1 (≈ 14 × 24 = 336 candles).
        var cutoff    = DateTimeOffset.UtcNow.AddDays(-H1EntryDays);
        var h1Entry   = h1Raw.Where(c => c.OpenTime >= cutoff).ToList();

        logger.LogInformation(
            "RealSetupCandleProvider: H4={H4}, H1={H1}, DXY={Dxy}, VIX={Vix} candles loaded for {Symbol}.",
            h4Candles.Count, h1Entry.Count, dxy.Count, vix.Count, symbol);

        return new InstitutionalSetupInput(
            symbol,
            exchange,
            h4Candles,
            h1Entry.Count > 0 ? h1Entry : h1Raw,
            dxy,
            vix,
            rsiPeriod);
    }

    private async Task<IReadOnlyList<Candle>> FetchSafeAsync(
        string symbol,
        string interval,
        string range,
        CancellationToken cancellationToken)
    {
        try
        {
            return await yahooService.GetHistoricalCandlesAsync(
                symbol, interval, range, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch candles for {Symbol} ({Interval}, {Range}). Using empty list.", symbol, interval, range);
            return Array.Empty<Candle>();
        }
    }
}
