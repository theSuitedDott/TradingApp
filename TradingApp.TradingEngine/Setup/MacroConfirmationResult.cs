using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Result of cross-asset confirmation using DXY (US dollar index) and VIX (volatility index).
/// </summary>
public sealed class MacroConfirmationResult
{
    /// <summary>
    /// Creates a macro confirmation result.
    /// </summary>
    /// <param name="isConfirmed">Whether DXY and VIX confirm the setup direction.</param>
    /// <param name="dxyBias">Detected DXY bias.</param>
    /// <param name="vixBias">Detected VIX bias.</param>
    /// <param name="detail">Human-readable explanation.</param>
    public MacroConfirmationResult(bool isConfirmed, MarketBias dxyBias, MarketBias vixBias, string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        IsConfirmed = isConfirmed;
        DxyBias = dxyBias;
        VixBias = vixBias;
        Detail = detail;
    }

    /// <summary>Whether DXY and VIX confirm the setup direction.</summary>
    public bool IsConfirmed { get; }

    /// <summary>Detected DXY bias.</summary>
    public MarketBias DxyBias { get; }

    /// <summary>Detected VIX bias.</summary>
    public MarketBias VixBias { get; }

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
