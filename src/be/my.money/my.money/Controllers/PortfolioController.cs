using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my.money.application.Portfolios.Dtos;
using my.money.application.Portfolios.Queries.GetDashboard;

namespace my.money.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public sealed class PortfolioController : ControllerBase
{
    private readonly GetDashboardHandler _getDashboardHandler;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        GetDashboardHandler getDashboardHandler,
        ILogger<PortfolioController> logger)
    {
        _getDashboardHandler = getDashboardHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get the authenticated user's portfolio dashboard
    /// Returns cash balance, current holdings, and available assets to trade
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        try
        {
            var query = new GetDashboardQuery();
            var dashboard = await _getDashboardHandler.HandleAsync(query, ct);

            _logger.LogInformation(
                "Dashboard retrieved: Cash={Cash} ARS, Holdings={HoldingCount}, Assets={AssetCount}",
                dashboard.CashBalanceAmount,
                ((IList<HoldingItemDto>)dashboard.Holdings).Count,
                ((IList<AssetItemDto>)dashboard.AvailableAssets).Count
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
