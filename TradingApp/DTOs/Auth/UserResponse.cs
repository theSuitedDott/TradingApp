namespace TradingApp.DTOs.Auth;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string Role,
    string? FirstName,
    string? LastName,
    DateTimeOffset CreatedAt);
