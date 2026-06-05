using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Confirms a directional setup against macro context (DXY and VIX) for risk assets.
/// </summary>
public interface IMacroConfirmationFilter
{
    /// <summary>
    /// Evaluates whether DXY and VIX confirm the setup direction.
    /// </summary>
    /// <param name="bias">Setup direction on the traded instrument.</param>
    /// <param name="dxyCandles">DXY candles (oldest first).</param>
    /// <param name="vixCandles">VIX candles (oldest first).</param>
    /// <returns>The confirmation result.</returns>
    MacroConfirmationResult Confirm(
        MarketBias bias,
        IReadOnlyList<Candle> dxyCandles,
        IReadOnlyList<Candle> vixCandles);
}
