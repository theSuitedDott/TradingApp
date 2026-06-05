using TradingApp.DTOs.Auth;

namespace TradingApp.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> RevokeAsync(RevokeTokenRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
