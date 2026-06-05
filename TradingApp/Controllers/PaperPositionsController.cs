using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Paper;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Virtual position endpoints for paper trading.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/paper/accounts/{accountId:guid}/positions")]
public sealed class PaperPositionsController(IPaperOrderService orderService) : ControllerBase
{
    /// <summary>Lists virtual positions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PositionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        Guid accountId,
        [FromQuery] bool openOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetPositionsAsync(GetUserId(), accountId, openOnly, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new ProblemDetails
        {
            Title = "Not found",
            Detail = result.ErrorMessage,
            Status = StatusCodes.Status404NotFound,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.Parse(claim!);
    }
}
