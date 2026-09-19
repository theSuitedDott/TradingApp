using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Input bundle for the institutional setup evaluation across multiple timeframes
/// and cross-asset context.
/// </summary>
public sealed class InstitutionalSetupInput
{
    /// <summary>
    /// Creates an institutional setup input.
    /// </summary>
    /// <param name="symbol">Traded instrument symbol.</param>
    /// <param name="exchange">Exchange / venue identifier.</param>
    /// <param name="higherTimeframeCandles">Higher-timeframe (e.g. H4) candles for the trend bias.</param>
    /// <param name="entryTimeframeCandles">Lower-timeframe candles for entry analysis.</param>
    /// <param name="dxyCandles">DXY candles for macro confirmation.</param>
    /// <param name="vixCandles">VIX candles for macro confirmation.</param>
    /// <param name="rsiPeriod">RSI period used for divergence analysis.</param>
    /// <param name="evaluationTime">Historical evaluation timestamp for backtests; live scans use the system clock.</param>
    public InstitutionalSetupInput(
        string symbol,
        string exchange,
        IReadOnlyList<Candle> higherTimeframeCandles,
        IReadOnlyList<Candle> entryTimeframeCandles,
        IReadOnlyList<Candle> dxyCandles,
        IReadOnlyList<Candle> vixCandles,
        int rsiPeriod = 14,
        DateTimeOffset? evaluationTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentNullException.ThrowIfNull(higherTimeframeCandles);
        ArgumentNullException.ThrowIfNull(entryTimeframeCandles);
        ArgumentNullException.ThrowIfNull(dxyCandles);
        ArgumentNullException.ThrowIfNull(vixCandles);
        ArgumentOutOfRangeException.ThrowIfLessThan(rsiPeriod, 2);

        Symbol = symbol;
        Exchange = exchange;
        HigherTimeframeCandles = higherTimeframeCandles;
        EntryTimeframeCandles = entryTimeframeCandles;
        DxyCandles = dxyCandles;
        VixCandles = vixCandles;
        RsiPeriod = rsiPeriod;
        EvaluationTime = evaluationTime;
    }

    /// <summary>Traded instrument symbol.</summary>
    public string Symbol { get; }

    /// <summary>Exchange / venue identifier.</summary>
    public string Exchange { get; }

    /// <summary>Higher-timeframe candles for the trend bias.</summary>
    public IReadOnlyList<Candle> HigherTimeframeCandles { get; }

    /// <summary>Lower-timeframe candles for entry analysis.</summary>
    public IReadOnlyList<Candle> EntryTimeframeCandles { get; }

    /// <summary>DXY candles for macro confirmation.</summary>
    public IReadOnlyList<Candle> DxyCandles { get; }

    /// <summary>VIX candles for macro confirmation.</summary>
    public IReadOnlyList<Candle> VixCandles { get; }

    /// <summary>RSI period used for divergence analysis.</summary>
    public int RsiPeriod { get; }

    /// <summary>Point-in-time for <see cref="InstitutionalSetupResult.DetectedAt"/> (backtest uses candle time).</summary>
    public DateTimeOffset? EvaluationTime { get; }
}
