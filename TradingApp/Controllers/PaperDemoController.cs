using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Paper;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Demo onboarding endpoints for virtual paper accounts and broker links.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/paper/demo")]
public sealed class PaperDemoController(IDemoTradingAccountService demoService) : ControllerBase
{
    /// <summary>Lists paper account demo presets.</summary>
    [HttpGet("presets/paper")]
    [ProducesResponseType(typeof(IReadOnlyList<DemoAccountPresetDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DemoAccountPresetDto>> GetPaperPresets()
        => Ok(demoService.GetPaperPresets());

    /// <summary>Lists demo broker link presets.</summary>
    [HttpGet("presets/broker")]
    [ProducesResponseType(typeof(IReadOnlyList<DemoAccountPresetDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DemoAccountPresetDto>> GetBrokerPresets()
        => Ok(demoService.GetBrokerPresets());

    /// <summary>Returns onboarding status for the current user.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(DemoTradingSetupStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await demoService.GetSetupStatusAsync(GetUserId(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a demo paper account from a preset.</summary>
    [HttpPost("provision")]
    [ProducesResponseType(typeof(PaperAccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Provision(
        [FromBody] ProvisionDemoAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await demoService.ProvisionPaperAccountAsync(GetUserId(), request, cancellationToken);
        return MapResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Creates a default demo paper account when none exists yet.</summary>
    [HttpPost("ensure")]
    [ProducesResponseType(typeof(PaperAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaperAccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Ensure(CancellationToken cancellationToken)
    {
        var hadAccount = (await demoService.GetSetupStatusAsync(GetUserId(), cancellationToken)).Value?.HasPaperAccount ?? false;
        var result = await demoService.EnsureDefaultPaperAccountAsync(GetUserId(), cancellationToken);
        var status = hadAccount ? StatusCodes.Status200OK : StatusCodes.Status201Created;
        return MapResult(result, status);
    }

    /// <summary>Links a demo broker connection (metadata only).</summary>
    [HttpPost("link-broker")]
    [ProducesResponseType(typeof(BrokerAccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> LinkBroker(
        [FromBody] LinkDemoBrokerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await demoService.LinkDemoBrokerAsync(GetUserId(), request, cancellationToken);
        return MapResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Lists linked demo broker connections.</summary>
    [HttpGet("brokers")]
    [ProducesResponseType(typeof(IReadOnlyList<BrokerAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrokers(CancellationToken cancellationToken)
    {
        var result = await demoService.GetLinkedBrokersAsync(GetUserId(), cancellationToken);
        return Ok(result.Value);
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
            PaperTradingErrorCodes.AccountNameExists or
                PaperTradingErrorCodes.BrokerLinkExists or
                PaperTradingErrorCodes.BrokerNameExists => Conflict(ToProblem(result)),
            PaperTradingErrorCodes.InvalidDemoPreset or
                PaperTradingErrorCodes.MissingBrokerAccountId => BadRequest(ToProblem(result)),
            _ => BadRequest(ToProblem(result))
        };
    }

    private static ProblemDetails ToProblem<T>(ServiceResult<T> result) =>
        new()
        {
            Title = "Demo trading setup error",
            Detail = result.ErrorMessage,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        };
}
