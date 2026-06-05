using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Indicators;

/// <summary>
/// Computes the Relative Strength Index (RSI) over a candle series.
/// </summary>
public interface IRsiCalculator
{
    /// <summary>
    /// Calculates RSI values aligned to the input candles.
    /// </summary>
    /// <param name="candles">Chronologically ordered candles (oldest first).</param>
    /// <param name="period">RSI lookback period (default 14).</param>
    /// <returns>
    /// RSI values aligned by index. Entries before enough data is available are <c>null</c>.
    /// </returns>
    IReadOnlyList<decimal?> Calculate(IReadOnlyList<Candle> candles, int period = 14);
}
