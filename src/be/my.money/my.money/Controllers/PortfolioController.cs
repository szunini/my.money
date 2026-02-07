using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my.money.application.Portfolios.Dtos;
using my.money.application.Portfolios.Queries.GetDashboard;
using my.money.application.Portfolios.Queries.GetDashboardValuation;

namespace my.money.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public sealed class PortfolioController : ControllerBase
{
    private readonly GetDashboardHandler _getDashboardHandler;
    private readonly GetDashboardValuationHandler _getDashboardValuationHandler;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        GetDashboardHandler getDashboardHandler,
        GetDashboardValuationHandler getDashboardValuationHandler,
        ILogger<PortfolioController> logger)
    {
        _getDashboardHandler = getDashboardHandler;
        _getDashboardValuationHandler = getDashboardValuationHandler;
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
}
