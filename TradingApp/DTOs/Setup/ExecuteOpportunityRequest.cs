using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Setup;

/// <summary>
/// Request to execute a detected opportunity as a virtual paper order.
/// </summary>
public sealed class ExecuteOpportunityRequest
{
    /// <summary>Paper account the order is placed on.</summary>
    [Required]
    public Guid AccountId { get; init; }

    /// <summary>Order quantity.</summary>
    [Range(0.00000001, 1_000_000_000)]
    public decimal Quantity { get; init; }
}
