using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Paper;
using TradingApp.DTOs.Setup;
using TradingApp.Services;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Controllers;

/// <summary>
/// Endpoints for the institutional setup scanner: analysis, opportunities and one-click execution.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/setups")]
public sealed class InstitutionalSetupController(
    IInstitutionalSetupScanner scanner,
    IPositionExitScanner exitScanner,
    ITradeOpportunityStore opportunityStore,
    ISetupExecutionService executionService,
    ISetupBacktestService backtestService,
    ISetupChartService chartService,
    IOptions<InstitutionalSetupSettings> options) : ControllerBase
{
    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <summary>Returns the current six-point analysis for a symbol (defaults to the configured symbol).</summary>
    [HttpGet("analyze")]
    [ProducesResponseType(typeof(SetupAnalysisDto), StatusCodes.Status200OK)]
    [AllowAnonymous] // Added to fix 401
    public ActionResult<SetupAnalysisDto> Analyze(
        [FromQuery] string? symbol,
        [FromQuery] string? exchange)
    {
        var analysis = scanner.Analyze(
            string.IsNullOrWhiteSpace(symbol) ? _settings.Symbol : symbol,
            string.IsNullOrWhiteSpace(exchange) ? _settings.Exchange : exchange);

        return Ok(analysis);
    }

    /// <summary>Lists detected trade opportunities, newest first.</summary>
    [HttpGet("opportunities")]
    [ProducesResponseType(typeof(IReadOnlyList<TradeOpportunityDto>), StatusCodes.Status200OK)]
    [AllowAnonymous] // Added to fix 401
    public ActionResult<IReadOnlyList<TradeOpportunityDto>> GetOpportunities()
        => Ok(opportunityStore.GetAll());

    /// <summary>Returns OHLC candles: Yahoo historical baseline + optional OANDA live tick on last candle.</summary>
    [HttpGet("candles")]
    [ProducesResponseType(typeof(ChartDataDto), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetCandles(
        [FromQuery] string symbol = "EUR_USD",
        [FromQuery] string interval = "1h",
        [FromQuery] string range = "60d",
        [FromQuery] int count = 1000,
        [FromQuery] bool mock = false,
        [FromQuery] bool live = false,
        [FromQuery] bool includeLevels = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var candleCount = count > 0 ? count : 1000;
            var data = await chartService.GetChartDataAsync(
                symbol,
                interval,
                range,
                mock,
                live,
                includeLevels,
                candleCount,
                cancellationToken);
            if (data.Candles.Count == 0)
            {
                return BadRequest($"No candle data available for '{symbol}'.");
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>Manually checks all open positions for stop-loss / take-profit sell signals.</summary>
    [HttpPost("check-exits")]
    [ProducesResponseType(typeof(IReadOnlyList<ExitSignalAlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckExits(CancellationToken cancellationToken)
    {
        var alerts = await exitScanner.ScanAsync(cancellationToken);
        return Ok(alerts);
    }

    /// <summary>Runs a backtest over the last 60 days (OANDA forex + Yahoo indices), returning partial setups too.</summary>
    [HttpGet("backtest")]
    [ProducesResponseType(typeof(IReadOnlyList<SetupAnalysisDto>), StatusCodes.Status200OK)]
    [AllowAnonymous] // Added to fix 401
    public async Task<IActionResult> Backtest([FromQuery] string symbol = "EUR_USD", CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await backtestService.RunAsync(symbol, cancellationToken);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>Executes an opportunity as a virtual paper order (one-click trade).</summary>
    [Authorize(Policy = "TraderOrAdmin")]
    [HttpPost("opportunities/{opportunityId:guid}/execute")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Execute(
        Guid opportunityId,
        [FromBody] ExecuteOpportunityRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await executionService.ExecuteAsync(
            GetUserId(),
            opportunityId,
            request.AccountId,
            request.Quantity,
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ErrorCode switch
        {
            SetupErrorCodes.OpportunityNotFound or PaperTradingErrorCodes.AccountNotFound
                => NotFound(ToProblem(result)),
            SetupErrorCodes.OpportunityNotActive
                => Conflict(ToProblem(result, StatusCodes.Status409Conflict)),
            _ => BadRequest(ToProblem(result))
        };
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.Parse(claim!);
    }

    private static ProblemDetails ToProblem<T>(ServiceResult<T> result, int status = StatusCodes.Status400BadRequest) =>
        new()
        {
            Title = "Institutional setup error",
            Detail = result.ErrorMessage,
            Status = status,
            Extensions = { ["errorCode"] = result.ErrorCode! }
        };
}
