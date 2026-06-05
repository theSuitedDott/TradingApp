using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Auth;
using TradingApp.Services;

namespace TradingApp.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RegisterAsync(request, GetClientIp(), cancellationToken);
        return MapAuthResult(result, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.LoginAsync(request, GetClientIp(), cancellationToken);
        return MapAuthResult(result, StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RefreshAsync(request, GetClientIp(), cancellationToken);
        return MapAuthResult(result, StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RevokeAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.ErrorMessage!);
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    private IActionResult MapAuthResult(ServiceResult<AuthResponse> result, int successStatusCode)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, result.Value);
        }

        return MapFailure(result.ErrorCode!, result.ErrorMessage!);
    }

    private IActionResult MapFailure(string errorCode, string message) =>
        errorCode switch
        {
            AuthErrorCodes.EmailAlreadyExists => Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["errorCode"] = errorCode }
            }),
            AuthErrorCodes.InvalidCredentials or
                AuthErrorCodes.InvalidRefreshToken or
                AuthErrorCodes.UserInactive => Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = message,
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["errorCode"] = errorCode }
            }),
            AuthErrorCodes.UserNotFound => NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = errorCode }
            }),
            _ => BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = message,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = errorCode }
            })
        };

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
