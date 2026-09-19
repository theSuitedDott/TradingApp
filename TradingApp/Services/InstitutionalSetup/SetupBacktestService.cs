using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.HistoricalData;
using static TradingApp.Services.HistoricalData.ForexSymbolNormalizer;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Backtests the institutional setup over historical candles (OANDA forex, Yahoo indices).
/// Uses a sliding window with 4-hour steps to keep response times reasonable.
/// </summary>
public sealed class SetupBacktestService(
    IHistoricalDataService historicalDataService,
    IInstitutionalSetupStrategy strategy,
    IOptions<InstitutionalSetupSettings> options,
    ILogger<SetupBacktestService> logger) : ISetupBacktestService
{
    private const int MinH1Candles = 30;
    private const int H1EntryDays = 14;
    private const int StepHours = 4;
    /// <summary>Mindestens 1/6 Bedingungen — damit einzelne Treffer im Chart sichtbar werden.</summary>
    private const decimal MinConfidence = 1m / 6m;
    private const int MaxResults = 250;

    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SetupAnalysisDto>> RunAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        logger.LogInformation("Starting setup backtest for {Symbol} (60d, step {Step}h).", symbol, StepHours);

        var h1Candles = await historicalDataService.GetHistoricalCandlesAsync(symbol, "1h", "60d", cancellationToken: cancellationToken);
        if (h1Candles.Count == 0)
        {
            throw new InvalidOperationException($"Could not fetch H1 data for symbol '{symbol}'.");
        }

        var dxyCandles = await historicalDataService.GetHistoricalCandlesAsync("DX-Y.NYB", "1d", "60d", cancellationToken: cancellationToken);
        var vixCandles = await historicalDataService.GetHistoricalCandlesAsync("^VIX", "1d", "60d", cancellationToken: cancellationToken);
        var h4Candles = CandleAggregator.Aggregate(h1Candles, TimeSpan.FromHours(4));

        var results = new List<SetupAnalysisDto>();
        var h4End = 0;
        var dxyEnd = 0;
        var vixEnd = 0;

        for (var i = MinH1Candles; i < h1Candles.Count; i += StepHours)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentTime = h1Candles[i - 1].OpenTime;

            while (h4End < h4Candles.Count && h4Candles[h4End].OpenTime <= currentTime)
            {
                h4End++;
            }

            while (dxyEnd < dxyCandles.Count && dxyCandles[dxyEnd].OpenTime <= currentTime)
            {
                dxyEnd++;
            }

            while (vixEnd < vixCandles.Count && vixCandles[vixEnd].OpenTime <= currentTime)
            {
                vixEnd++;
            }

            if (h4End < 10 || dxyEnd < 3 || vixEnd < 3)
            {
                continue;
            }

            var input = new InstitutionalSetupInput(
                IsOandaForex(symbol) ? ToOandaInstrument(symbol) : symbol,
                IsOandaForex(symbol) ? "OANDA" : "Yahoo",
                new CandleWindow(h4Candles, h4End),
                BuildH1EntryWindow(h1Candles, i, currentTime),
                new CandleWindow(dxyCandles, dxyEnd),
                new CandleWindow(vixCandles, vixEnd),
                _settings.RsiPeriod,
                currentTime);

            var eval = strategy.EvaluateAllConditions(input);
            if (eval.Confidence >= MinConfidence)
            {
                results.Add(SetupMapper.ToAnalysis(eval));
                if (results.Count >= MaxResults)
                {
                    break;
                }
            }
        }

        logger.LogInformation(
            "Backtest for {Symbol} finished: {Count} result(s) from {Candles} H1 candles.",
            symbol,
            results.Count,
            h1Candles.Count);

        return results;
    }

    /// <summary>
    /// Last <see cref="H1EntryDays"/> of H1 candles up to <paramref name="endExclusive"/>,
    /// aligned with <see cref="RealSetupCandleProvider"/>.
    /// </summary>
    private static IReadOnlyList<Candle> BuildH1EntryWindow(
        IReadOnlyList<Candle> h1Candles,
        int endExclusive,
        DateTimeOffset currentTime)
    {
        var cutoff = currentTime.AddDays(-H1EntryDays);
        var start = 0;
        for (var j = endExclusive - 1; j >= 0; j--)
        {
            if (h1Candles[j].OpenTime < cutoff)
            {
                start = j + 1;
                break;
            }
        }

        var count = endExclusive - start;
        return count > 0
            ? new CandleRangeWindow(h1Candles, start, count)
            : new CandleWindow(h1Candles, endExclusive);
    }
}
