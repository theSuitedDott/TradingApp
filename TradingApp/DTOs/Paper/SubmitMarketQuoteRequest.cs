using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Paper;

/// <summary>
/// Request to submit a realtime market quote tick.
/// </summary>
public sealed class SubmitMarketQuoteRequest
{
    [Required]
    [MaxLength(32)]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Exchange { get; init; } = string.Empty;

    [Range(0.000001, 1_000_000_000)]
    public decimal Price { get; init; }

    public DateTimeOffset? Timestamp { get; init; }
}
