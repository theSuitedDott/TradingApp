using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.OandaOrder;
using TradingApp.Services;
using TradingApp.Services.OandaOrder;

namespace TradingApp.Controllers;

/// <summary>
/// Manages live market orders on the configured OANDA account.
/// </summary>
[ApiController]
[Authorize(Policy = "TraderOrAdmin")]
[Route("api/v1/oanda")]
public sealed class OandaOrderController(IOandaOrderService orderService) : ControllerBase
{
    /// <summary>Returns all currently open trades on the OANDA account.</summary>
    [HttpGet("trades")]
    [ProducesResponseType(typeof(IReadOnlyList<OandaTradeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenTrades(CancellationToken cancellationToken)
    {
        var trades = await orderService.GetOpenTradesAsync(cancellationToken);
        return Ok(trades);
    }

    /// <summary>Places a live market order on OANDA.</summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(OandaTradeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOandaOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var lotsInUnits = Math.Round(request.Lots * 100_000m, 0);
        var units = request.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase)
            ? lotsInUnits
            : -lotsInUnits;

        var instrument = Services.HistoricalData.ForexSymbolNormalizer.ToOandaInstrument(request.Symbol);

        var result = await orderService.PlaceMarketOrderAsync(
            instrument,
            units,
            request.StopLossPrice,
            request.TakeProfitPrice,
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ErrorCode switch
        {
            OandaOrderErrorCodes.NotConfigured => BadRequest(ToProblem(result)),
            _ => StatusCode(StatusCodes.Status502BadGateway, ToProblem(result))
        };
    }

    /// <summary>Closes an open trade entirely.</summary>
    /// <param name="tradeId">OANDA trade identifier (numeric string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("trades/{tradeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseTrade(
        string tradeId,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CloseTradeAsync(tradeId, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ErrorCode switch
        {
            OandaOrderErrorCodes.NotConfigured => BadRequest(ToProblem(result)),
            _ => StatusCode(StatusCodes.Status502BadGateway, ToProblem(result))
        };
    }

    private static ProblemDetails ToProblem<T>(ServiceResult<T> result, int status = StatusCodes.Status400BadRequest) =>
        new()
        {
            Title = "OANDA order error",
            Detail = result.ErrorMessage,
            Status = status,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        };
}
