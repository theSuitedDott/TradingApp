using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.OandaOrder;

/// <summary>
/// Request body for placing a live market order on OANDA.
/// </summary>
public sealed class PlaceOandaOrderRequest
{
    /// <summary>OANDA instrument identifier (e.g. EUR_USD or GBP_USD).</summary>
    [Required]
    public string Symbol { get; init; } = string.Empty;

    /// <summary>Order direction: "Buy" or "Sell".</summary>
    [Required]
    public string Side { get; init; } = string.Empty;

    /// <summary>
    /// Trade size in standard lots (1 lot = 100,000 units).
    /// Must be greater than zero.
    /// </summary>
    [Range(0.001, 100)]
    public decimal Lots { get; init; } = 0.01m;

    /// <summary>Stop-loss price. Null means no stop-loss order is attached.</summary>
    public decimal? StopLossPrice { get; init; }

    /// <summary>Take-profit price. Null means no take-profit order is attached.</summary>
    public decimal? TakeProfitPrice { get; init; }
}
