using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Paper;

/// <summary>
/// Request to create a paper trade account from a demo preset.
/// </summary>
public sealed class ProvisionDemoAccountRequest
{
    [Required]
    [MaxLength(64)]
    public string PresetId { get; init; } = string.Empty;

    [MaxLength(128)]
    public string? CustomName { get; init; }

    public bool IsDefault { get; init; } = true;
}
