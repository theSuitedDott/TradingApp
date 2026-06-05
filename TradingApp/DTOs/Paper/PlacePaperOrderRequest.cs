using System.ComponentModel.DataAnnotations;
using TradingApp.Entities.Enums;

namespace TradingApp.DTOs.Paper;

/// <summary>
/// Request to place a virtual paper order.
/// </summary>
public sealed class PlacePaperOrderRequest
{
    [Required]
    [MaxLength(32)]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Exchange { get; init; } = string.Empty;

    [Required]
    public OrderSide Side { get; init; }

    [Required]
    public OrderType Type { get; init; }

    [Range(0.00000001, 1_000_000_000)]
    public decimal Quantity { get; init; }

    [Range(0.000001, 1_000_000_000)]
    public decimal? LimitPrice { get; init; }

    [MaxLength(64)]
    public string? ClientOrderId { get; init; }

    /// <summary>Optional stop-loss for the resulting long position.</summary>
    [Range(0.000001, 1_000_000_000)]
    public decimal? StopLossPrice { get; init; }

    /// <summary>Optional take-profit for the resulting long position.</summary>
    [Range(0.000001, 1_000_000_000)]
    public decimal? TakeProfitPrice { get; init; }
}
