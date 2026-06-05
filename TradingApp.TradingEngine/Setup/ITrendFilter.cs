using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Determines the higher-timeframe (e.g. H4) directional bias from market structure.
/// </summary>
public interface ITrendFilter
{
    /// <summary>
    /// Derives the directional bias from the candle structure.
    /// </summary>
    /// <param name="candles">Higher-timeframe candles (oldest first).</param>
    /// <returns>The detected bias, or <see cref="MarketBias.Neutral"/> when unclear.</returns>
    MarketBias DetermineBias(IReadOnlyList<Candle> candles);
}
