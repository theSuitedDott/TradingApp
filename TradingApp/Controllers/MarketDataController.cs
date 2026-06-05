using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingApp.DTOs.Paper;
using TradingApp.Services;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Market quote ingestion and retrieval for paper trading simulations.
/// </summary>
[ApiController]
[Route("api/v1/market")]
public sealed class MarketDataController(IMarketDataService marketDataService) : ControllerBase
{
    /// <summary>Submits a realtime quote tick (triggers fills and PnL updates).</summary>
    [Authorize(Policy = "TraderOrAdmin")]
    [HttpPost("quotes")]
    [ProducesResponseType(typeof(MarketQuoteProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitQuote(
        [FromBody] SubmitMarketQuoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await marketDataService.SubmitQuoteAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Detail = result.ErrorMessage,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        });
    }

    /// <summary>Gets the latest quote for an instrument.</summary>
    [Authorize]
    [HttpGet("quotes/{symbol}/{exchange}")]
    [ProducesResponseType(typeof(MarketQuoteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuote(string symbol, string exchange, CancellationToken cancellationToken)
    {
        var result = await marketDataService.GetQuoteAsync(symbol, exchange, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new ProblemDetails
        {
            Detail = result.ErrorMessage,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        });
    }
}
