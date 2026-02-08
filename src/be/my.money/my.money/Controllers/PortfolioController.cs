using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using my.money.application.Portfolios.Commands.BuyAsset;
using my.money.application.Portfolios.Commands.SellAsset;
using my.money.application.Portfolios.Dtos;
using my.money.application.Portfolios.Queries.GetDashboard;
using my.money.application.Portfolios.Queries.GetDashboardValuation;
using my.money.application.Portfolios.Queries.GetPortfolioValuationAsOf;
using my.money.application.Portfolios.Queries.TradePreview;

namespace my.money.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public sealed class PortfolioController : ControllerBase
{
    private readonly GetDashboardHandler _getDashboardHandler;
    private readonly GetDashboardValuationHandler _getDashboardValuationHandler;
    private readonly GetPortfolioValuationAsOfHandler _getPortfolioValuationAsOfHandler;
    private readonly BuyAssetHandler _buyAssetHandler;
    private readonly SellAssetHandler _sellAssetHandler;
    private readonly TradePreviewHandler _tradePreviewHandler;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        GetDashboardHandler getDashboardHandler,
        GetDashboardValuationHandler getDashboardValuationHandler,
        GetPortfolioValuationAsOfHandler getPortfolioValuationAsOfHandler,
        BuyAssetHandler buyAssetHandler,
        SellAssetHandler sellAssetHandler,
        TradePreviewHandler tradePreviewHandler,
        ILogger<PortfolioController> logger)
    {
        _getDashboardHandler = getDashboardHandler;
        _getDashboardValuationHandler = getDashboardValuationHandler;
        _getPortfolioValuationAsOfHandler = getPortfolioValuationAsOfHandler;
        _buyAssetHandler = buyAssetHandler;
        _sellAssetHandler = sellAssetHandler;
        _tradePreviewHandler = tradePreviewHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get the authenticated user's portfolio dashboard with valuation
    /// Returns cash balance, holdings valuation with latest prices, and tradable assets
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardValuationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        try
        {
            var query = new GetDashboardValuationQuery();
            var dashboard = await _getDashboardValuationHandler.HandleAsync(query, ct);

            _logger.LogInformation(
                "Dashboard retrieved: Cash={Cash} ARS, Holdings={HoldingCount}, Assets={AssetCount}, Total={Total} ARS",
                dashboard.CashBalance,
                dashboard.Holdings.Count,
                dashboard.TradableAssets.Count,
                dashboard.TotalPortfolioValue
            );

            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized dashboard access attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard");
            return StatusCode(500, new { message = "An error occurred while retrieving the dashboard" });
        }
    }

    /// <summary>
    /// Get the authenticated user's portfolio valuation at a specific historical point in time
    /// Returns portfolio value, cash balance, and holdings valuation based on historical quotes
    /// </summary>
    [HttpGet("valuation/asof")]
    [ProducesResponseType(typeof(PortfolioValuationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetValuationAsOf([FromQuery] DateTime asOf, CancellationToken ct)
    {
        try
        {
            // Parse as UTC datetime
            var asOfUtc = DateTime.SpecifyKind(asOf, DateTimeKind.Utc);

            var query = new GetPortfolioValuationAsOfQuery(asOfUtc);
            var valuation = await _getPortfolioValuationAsOfHandler.HandleAsync(query, ct);

            _logger.LogInformation(
                "Portfolio valuation retrieved for asOf={AsOf}: Cash={Cash}, Holdings={HoldingCount}, Total={Total}",
                asOfUtc,
                valuation.CashBalanceAmount,
                valuation.Holdings.Count,
                valuation.TotalPortfolioValue
            );

            return Ok(valuation);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized valuation access attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Portfolio not found"))
        {
            _logger.LogWarning(ex, "Portfolio not found for authenticated user");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No historical quote found"))
        {
            _logger.LogWarning(ex, "Missing historical quotes for valuation at requested date");
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid valuation request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving portfolio valuation");
            return StatusCode(500, new { message = "An error occurred while retrieving the portfolio valuation" });
        }
    }

    /// <summary>
    /// Buy an asset for the authenticated user's portfolio
    /// </summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Buy([FromBody] TradeRequest request, CancellationToken ct)
    {
        try
        {
            var command = new BuyAssetCommand(request.AssetId, request.Quantity);
            var response = await _buyAssetHandler.HandleAsync(command, ct);
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized buy attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid buy request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Asset not found: {AssetId}", request.AssetId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient cash"))
        {
            _logger.LogWarning(ex, "Insufficient cash for buy");
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during buy operation");
            return Conflict(new { message = "The operation failed due to a concurrent modification. Please retry." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing buy");
            return StatusCode(500, new { message = "An error occurred while processing the purchase" });
        }
    }

    /// <summary>
    /// Sell an asset from the authenticated user's portfolio
    /// </summary>
    [HttpPost("sell")]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Sell([FromBody] TradeRequest request, CancellationToken ct)
    {
        try
        {
            var command = new SellAssetCommand(request.AssetId, request.Quantity);
            var response = await _sellAssetHandler.HandleAsync(command, ct);
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized sell attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid sell request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Asset or portfolio not found: {AssetId}", request.AssetId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient quantity"))
        {
            _logger.LogWarning(ex, "Insufficient quantity for sell");
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during sell operation");
            return Conflict(new { message = "The operation failed due to a concurrent modification. Please retry." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sell");
            return StatusCode(500, new { message = "An error occurred while processing the sale" });
        }
    }

    /// <summary>
    /// Preview a trade for the authenticated user's portfolio
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(TradePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview([FromBody] TradePreviewRequest request, CancellationToken ct)
    {
        try
        {
            var query = new TradePreviewQuery(request.AssetId, request.Quantity, request.Side);
            var response = await _tradePreviewHandler.HandleAsync(query, ct);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized preview attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid preview request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Asset not found: {AssetId}", request.AssetId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing preview");
            return StatusCode(500, new { message = "An error occurred while processing the preview" });
        }
    }
}

