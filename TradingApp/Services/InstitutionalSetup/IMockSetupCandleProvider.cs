using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Provides deterministic mock multi-timeframe candle data for the institutional setup
/// scanner. This is a demo data source (not production market data) that produces a
/// textbook bullish setup so the full notification and trade-opportunity flow is visible.
/// </summary>
public interface IMockSetupCandleProvider
{
    /// <summary>
    /// Builds the institutional setup input (H4, entry, DXY and VIX candles) for a symbol.
    /// </summary>
    /// <param name="symbol">Instrument symbol.</param>
    /// <param name="exchange">Exchange / venue.</param>
    /// <param name="rsiPeriod">RSI period for divergence analysis.</param>
    /// <returns>The assembled setup input.</returns>
    InstitutionalSetupInput BuildInput(string symbol, string exchange, int rsiPeriod);
}
