using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Paper;

/// <summary>
/// Request to create a paper trade account.
/// </summary>
public sealed class CreatePaperAccountRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 100_000_000)]
    public decimal InitialBalance { get; init; } = 100_000m;

    [MaxLength(3)]
    public string BaseCurrency { get; init; } = "EUR";

    public bool IsDefault { get; init; }
}
