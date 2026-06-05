using System.ComponentModel.DataAnnotations;

namespace TradingApp.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
