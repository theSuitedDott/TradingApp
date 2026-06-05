namespace TradingApp.TradingEngine.Models;

/// <summary>
/// Directional market bias derived from higher-timeframe structure.
/// </summary>
public enum MarketBias
{
    /// <summary>No clear directional bias.</summary>
    Neutral = 0,

    /// <summary>Uptrend (higher highs and higher lows).</summary>
    Bullish = 1,

    /// <summary>Downtrend (lower highs and lower lows).</summary>
    Bearish = 2
}
