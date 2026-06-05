using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects a liquidity sweep (inducement): the final corrective push runs a prior
/// swing level and price then reclaims it back in the trend direction.
/// </summary>
public interface ILiquiditySweepDetector
{
    /// <summary>
    /// Analyses the candle series for a liquidity sweep.
    /// </summary>
    /// <param name="candles">Entry-timeframe candles (oldest first).</param>
    /// <param name="bias">Higher-timeframe trend bias.</param>
    /// <returns>The sweep result, or <c>null</c> when no sweep occurred.</returns>
    SweepResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias);
}
