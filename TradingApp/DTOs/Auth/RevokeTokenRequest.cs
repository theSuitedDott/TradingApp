using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Auth;

public sealed class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
