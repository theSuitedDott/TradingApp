using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Data;
using TradingApp.DTOs.Auth;
using TradingApp.Entities;

namespace TradingApp.Services;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ServiceResult<AuthResponse>.Failure(
                AuthErrorCodes.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            Role = UserRole.Trader,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User registered with id {UserId}", user.Id);

        return ServiceResult<AuthResponse>.Success(
            await IssueTokensAsync(user, ipAddress, cancellationToken));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthResponse>.Failure(
                AuthErrorCodes.InvalidCredentials,
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthResponse>.Failure(
                AuthErrorCodes.UserInactive,
                "This account has been deactivated.");
        }

        return ServiceResult<AuthResponse>.Success(
            await IssueTokensAsync(user, ipAddress, cancellationToken));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return ServiceResult<AuthResponse>.Failure(
                AuthErrorCodes.InvalidRefreshToken,
                "Invalid or expired refresh token.");
        }

        var user = storedToken.User;
        if (!user.IsActive)
        {
            return ServiceResult<AuthResponse>.Failure(
                AuthErrorCodes.UserInactive,
                "This account has been deactivated.");
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var (newRefreshToken, newRefreshEntity) = CreateRefreshTokenEntity(user.Id, ipAddress);
        storedToken.ReplacedByTokenHash = newRefreshEntity.TokenHash;

        dbContext.RefreshTokens.Add(newRefreshEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user);

        return ServiceResult<AuthResponse>.Success(
            new AuthResponse(accessToken, newRefreshToken, expiresAt, user.ToUserResponse()));
    }

    public async Task<ServiceResult<bool>> RevokeAsync(
        RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return ServiceResult<bool>.Failure(
                AuthErrorCodes.InvalidRefreshToken,
                "Invalid or expired refresh token.");
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<UserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<UserResponse>.Failure(
                AuthErrorCodes.UserNotFound,
                "User not found.");
        }

        return ServiceResult<UserResponse>.Success(user.ToUserResponse());
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user);
        var (refreshToken, refreshEntity) = CreateRefreshTokenEntity(user.Id, ipAddress);

        dbContext.RefreshTokens.Add(refreshEntity);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, expiresAt, user.ToUserResponse());
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshTokenEntity(Guid userId, string? ipAddress)
    {
        var rawToken = tokenService.GenerateRefreshToken();
        var now = DateTimeOffset.UtcNow;

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = TokenHasher.Hash(rawToken),
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = now,
            CreatedByIp = ipAddress
        };

        return (rawToken, entity);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
