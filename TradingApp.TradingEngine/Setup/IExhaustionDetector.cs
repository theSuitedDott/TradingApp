using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects an exhausting counter-trend correction that unfolds in three
/// progressively smaller pushes accompanied by an RSI divergence.
/// </summary>
public interface IExhaustionDetector
{
    /// <summary>
    /// Analyses the lower-timeframe correction for exhaustion.
    /// </summary>
    /// <param name="candles">Entry-timeframe candles (oldest first).</param>
    /// <param name="bias">Higher-timeframe trend bias the correction runs against.</param>
    /// <param name="rsi">RSI values aligned to <paramref name="candles"/> by index.</param>
    /// <returns>The exhaustion result, or <c>null</c> when the pattern is absent.</returns>
    ExhaustionResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias, IReadOnlyList<decimal?> rsi);
}
