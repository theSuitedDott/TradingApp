using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Paper;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Paper trade account and portfolio endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/paper/accounts")]
public sealed class PaperAccountsController(IPaperTradeAccountService accountService) : ControllerBase
{
    /// <summary>Creates a new paper trade account with virtual balance.</summary>
    [Authorize(Policy = "TraderOrAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(PaperAccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaperAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await accountService.CreateAccountAsync(GetUserId(), request, cancellationToken);
        return MapResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Lists paper trade accounts for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaperAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await accountService.GetAccountsAsync(GetUserId(), cancellationToken);
        return MapResult(result, StatusCodes.Status200OK);
    }

    /// <summary>Gets virtual portfolio summary including PnL.</summary>
    [HttpGet("{accountId:guid}/portfolio")]
    [ProducesResponseType(typeof(PortfolioSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolio(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await accountService.GetPortfolioSummaryAsync(GetUserId(), accountId, cancellationToken);
        return MapResult(result, StatusCodes.Status200OK);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.Parse(claim!);
    }

    private IActionResult MapResult<T>(ServiceResult<T> result, int successStatus)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatus, result.Value);
        }

        return result.ErrorCode switch
        {
            PaperTradingErrorCodes.AccountNotFound => NotFound(ToProblem(result)),
            PaperTradingErrorCodes.AccountNameExists => Conflict(ToProblem(result)),
            _ => BadRequest(ToProblem(result))
        };
    }

    private ProblemDetails ToProblem<T>(ServiceResult<T> result) =>
        new()
        {
            Title = "Paper trading error",
            Detail = result.ErrorMessage,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        };
}
