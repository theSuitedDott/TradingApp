using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Confirms risk-asset setups using the inverse correlation to DXY and VIX:
/// a long is confirmed when both the dollar index and volatility are falling,
/// a short when both are rising.
/// </summary>
public sealed class DxyVixConfirmationFilter : IMacroConfirmationFilter
{
    private readonly ITrendFilter _trendFilter;

    /// <summary>
    /// Creates a DXY/VIX confirmation filter.
    /// </summary>
    /// <param name="trendFilter">Trend filter used to derive DXY and VIX bias.</param>
    public DxyVixConfirmationFilter(ITrendFilter trendFilter)
    {
        ArgumentNullException.ThrowIfNull(trendFilter);
        _trendFilter = trendFilter;
    }

    /// <inheritdoc />
    public MacroConfirmationResult Confirm(
        MarketBias bias,
        IReadOnlyList<Candle> dxyCandles,
        IReadOnlyList<Candle> vixCandles)
    {
        ArgumentNullException.ThrowIfNull(dxyCandles);
        ArgumentNullException.ThrowIfNull(vixCandles);

        var dxyBias = _trendFilter.DetermineBias(dxyCandles);
        var vixBias = _trendFilter.DetermineBias(vixCandles);

        if (bias == MarketBias.Neutral)
        {
            return new MacroConfirmationResult(false, dxyBias, vixBias, "No directional bias to confirm.");
        }

        // Risk-on (long) needs falling DXY and VIX; risk-off (short) needs rising DXY and VIX.
        var required = bias == MarketBias.Bullish ? MarketBias.Bearish : MarketBias.Bullish;
        var confirmed = dxyBias == required && vixBias == required;

        var detail = confirmed
            ? $"DXY {dxyBias} and VIX {vixBias} confirm the {bias} setup."
            : $"DXY {dxyBias} / VIX {vixBias} do not confirm the {bias} setup (need both {required}).";

        return new MacroConfirmationResult(confirmed, dxyBias, vixBias, detail);
    }
}
