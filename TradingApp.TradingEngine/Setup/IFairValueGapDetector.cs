using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects the Fair Value Gap (three-candle imbalance) created by a displacement candle.
/// The gap forms the entry zone.
/// </summary>
public interface IFairValueGapDetector
{
    /// <summary>
    /// Finds the Fair Value Gap around the displacement candle.
    /// </summary>
    /// <param name="candles">Entry-timeframe candles (oldest first).</param>
    /// <param name="bias">Higher-timeframe trend bias.</param>
    /// <param name="displacementIndex">Index of the displacement (middle) candle.</param>
    /// <returns>The FVG zone, or <c>null</c> when no imbalance exists.</returns>
    PriceZone? Detect(IReadOnlyList<Candle> candles, MarketBias bias, int displacementIndex);
}
