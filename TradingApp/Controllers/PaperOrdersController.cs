using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Paper;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Virtual order endpoints for paper trading.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/paper/accounts/{accountId:guid}/orders")]
public sealed class PaperOrdersController(IPaperOrderService orderService) : ControllerBase
{
    /// <summary>Places a virtual order.</summary>
    [Authorize(Policy = "TraderOrAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Place(
        Guid accountId,
        [FromBody] PlacePaperOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await orderService.PlaceOrderAsync(GetUserId(), accountId, request, cancellationToken);
        return MapResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Lists orders for the paper account.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrdersAsync(GetUserId(), accountId, cancellationToken);
        return MapResult(result, StatusCodes.Status200OK);
    }

    /// <summary>Cancels a pending virtual order.</summary>
    [Authorize(Policy = "TraderOrAdmin")]
    [HttpDelete("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CancelOrderAsync(GetUserId(), accountId, orderId, cancellationToken);
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
            PaperTradingErrorCodes.AccountNotFound or PaperTradingErrorCodes.OrderNotFound => NotFound(ToProblem(result)),
            PaperTradingErrorCodes.InsufficientFunds or
                PaperTradingErrorCodes.InsufficientPosition or
                PaperTradingErrorCodes.NoMarketPrice => BadRequest(ToProblem(result, StatusCodes.Status400BadRequest)),
            PaperTradingErrorCodes.DuplicateClientOrderId => Conflict(ToProblem(result, StatusCodes.Status409Conflict)),
            PaperTradingErrorCodes.InvalidOrderState or PaperTradingErrorCodes.AccountInactive
                => BadRequest(ToProblem(result, StatusCodes.Status400BadRequest)),
            _ => BadRequest(ToProblem(result))
        };
    }

    private static ProblemDetails ToProblem<T>(ServiceResult<T> result, int status = StatusCodes.Status400BadRequest) =>
        new()
        {
            Title = "Paper order error",
            Detail = result.ErrorMessage,
            Status = status,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        };
}
