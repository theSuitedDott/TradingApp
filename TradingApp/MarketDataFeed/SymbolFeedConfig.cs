using System.ComponentModel.DataAnnotations;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Configuration for a single instrument in the market data feed.
/// </summary>
public sealed class SymbolFeedConfig
{
    /// <summary>Instrument symbol (e.g. "SAP").</summary>
    [Required]
    [MaxLength(32)]
    public string Symbol { get; init; } = string.Empty;

    /// <summary>Exchange (e.g. "XETRA", "NYSE").</summary>
    [Required]
    [MaxLength(32)]
    public string Exchange { get; init; } = string.Empty;

    /// <summary>Starting price for the simulated feed.</summary>
    [Range(0.000001, double.MaxValue)]
    public decimal SimulatedBasePrice { get; init; } = 100m;

    /// <summary>
    /// Maximum relative price move per tick for the simulated feed (0.01 = ±1 %).
    /// </summary>
    [Range(0, 0.5)]
    public decimal SimulatedVolatilityPercent { get; init; } = 0.005m;
}
