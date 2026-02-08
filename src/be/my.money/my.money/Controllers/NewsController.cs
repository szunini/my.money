using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my.money.application.News.Commands.RefreshEconomicNews;
using my.money.application.News.Dtos;
using my.money.application.News.Queries.GetAssetNews;

namespace my.money.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly RefreshEconomicNewsHandler _refreshHandler;
    private readonly GetAssetNewsHandler _getAssetNewsHandler;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        RefreshEconomicNewsHandler refreshHandler,
        GetAssetNewsHandler getAssetNewsHandler,
        ILogger<NewsController> logger)
    {
        _refreshHandler = refreshHandler;
        _getAssetNewsHandler = getAssetNewsHandler;
        _logger = logger;
    }

    /// <summary>
    /// Manually refresh economic news from RSS feeds and analyze for asset mentions
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]  // Consider restricting to admin in production
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshEconomicNews(
        [FromQuery] decimal? confidenceThreshold = null,
        CancellationToken ct = default)
    {
        try
        {
            var command = new RefreshEconomicNewsCommand
            {
                ConfidenceThreshold = confidenceThreshold ?? 0.55m
            };

            await _refreshHandler.ExecuteAsync(command, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing economic news");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error refreshing news",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get related news for a specific asset
    /// </summary>
    [HttpGet("assets/{assetId:guid}/mentions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<AssetMentionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetNews(
        Guid assetId,
        [FromQuery] int maxDays = 30,
        CancellationToken ct = default)
    {
        try
        {
            var query = new GetAssetNewsQuery
            {
                AssetId = assetId,
                MaxDays = maxDays
            };

            var mentions = await _getAssetNewsHandler.HandleAsync(query, ct);
            return Ok(mentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset news");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving news",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }
}
