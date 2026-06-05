using TradingApp.Constants;
using TradingApp.DTOs.Auth;
using TradingApp.Entities;

namespace TradingApp.Services;

internal static class AuthMapper
{
    public static UserResponse ToUserResponse(this User user) =>
        new(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.FirstName,
            user.LastName,
            user.CreatedAt);

    public static bool TryParseRole(string role, out UserRole userRole)
    {
        if (!AppRoles.All.Contains(role))
        {
            userRole = default;
            return false;
        }

        userRole = Enum.Parse<UserRole>(role, ignoreCase: false);
        return true;
    }
}
