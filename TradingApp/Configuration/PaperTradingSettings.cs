using System.ComponentModel.DataAnnotations;

namespace TradingApp.Configuration;

/// <summary>
/// Configuration for virtual paper trading execution.
/// </summary>
public sealed class PaperTradingSettings
{
    public const string SectionName = "PaperTrading";

    /// <summary>Commission rate applied per fill (e.g. 0.001 = 0.1%).</summary>
    [Range(0, 1)]
    public decimal CommissionRate { get; init; } = 0.001m;

    /// <summary>Minimum commission amount per fill.</summary>
    [Range(0, 1000)]
    public decimal MinimumCommission { get; init; } = 1m;
}
