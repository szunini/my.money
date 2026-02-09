using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using my.money.application.Portfolios.Commands.BuyAsset;
using my.money.application.Portfolios.Commands.SellAsset;
using my.money.application.Portfolios.Dtos;
using my.money.application.Portfolios.Queries.GetDashboardValuation;

namespace my.money.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public sealed class PortfolioController : ControllerBase
{
    private readonly GetDashboardValuationHandler _getDashboardValuationHandler;
    private readonly BuyAssetHandler _buyAssetHandler;
    private readonly SellAssetHandler _sellAssetHandler;
   
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
      
        GetDashboardValuationHandler getDashboardValuationHandler,
        BuyAssetHandler buyAssetHandler,
        SellAssetHandler sellAssetHandler,
       
        ILogger<PortfolioController> logger)
    {
      
        _getDashboardValuationHandler = getDashboardValuationHandler;
        _buyAssetHandler = buyAssetHandler;
        _sellAssetHandler = sellAssetHandler;
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

    
}

