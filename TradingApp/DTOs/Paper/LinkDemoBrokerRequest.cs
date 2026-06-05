using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Paper;

/// <summary>
/// Request to link a demo broker connection (no live trading).
/// </summary>
public sealed class LinkDemoBrokerRequest
{
    [Required]
    [MaxLength(64)]
    public string PresetId { get; init; } = string.Empty;

    [MaxLength(128)]
    public string? DisplayName { get; init; }

    [MaxLength(128)]
    public string? ExternalAccountId { get; init; }
}
