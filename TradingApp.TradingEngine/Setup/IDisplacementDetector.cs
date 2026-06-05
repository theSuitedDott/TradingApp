using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects a displacement candle: an unusually large body in the trend direction
/// signalling an aggressive return of the dominant side.
/// </summary>
public interface IDisplacementDetector
{
    /// <summary>
    /// Finds the most recent qualifying displacement candle.
    /// </summary>
    /// <param name="candles">Entry-timeframe candles (oldest first).</param>
    /// <param name="bias">Higher-timeframe trend bias the candle must align with.</param>
    /// <returns>The displacement result, or <c>null</c> when none qualifies.</returns>
    DisplacementResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias);
}
