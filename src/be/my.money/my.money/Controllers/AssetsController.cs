using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my.money.application.Assets.Dtos;
using my.money.application.Assets.Queries.GetAssetDetail;
using my.money.application.Ports.Queries;

namespace my.money.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetQueryService _assetQueryService;
    private readonly GetAssetDetailHandler _getAssetDetailHandler;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetQueryService assetQueryService,
        GetAssetDetailHandler getAssetDetailHandler,
        ILogger<AssetsController> logger)
    {
        _assetQueryService = assetQueryService;
        _getAssetDetailHandler = getAssetDetailHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get all available assets (stocks and bonds)
    /// </summary>
    [HttpGet]
    [AllowAnonymous] // Allow public access to asset list
    [ProducesResponseType(typeof(IEnumerable<AssetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAssets(CancellationToken ct)
    {
        var assets = await _assetQueryService.GetAllAssetsAsync(ct);
        
        _logger.LogInformation("Retrieved {Count} assets", ((IList<AssetDto>)assets).Count);
        
        return Ok(assets);
    }

    /// <summary>
    /// Get a specific asset by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetById(Guid id, CancellationToken ct)
    {
        var asset = await _assetQueryService.GetAssetByIdAsync(id, ct);
        
        if (asset is null)
        {
            _logger.LogWarning("Asset with ID {AssetId} not found", id);
            return NotFound(new { message = $"Asset with ID {id} not found" });
        }
        
        return Ok(asset);
    }

    /// <summary>
    /// Get a specific asset by ticker symbol
    /// </summary>
    [HttpGet("ticker/{ticker}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetByTicker(string ticker, CancellationToken ct)
    {
        var asset = await _assetQueryService.GetAssetByTickerAsync(ticker, ct);
        
        if (asset is null)
        {
            _logger.LogWarning("Asset with ticker {Ticker} not found", ticker);
            return NotFound(new { message = $"Asset with ticker '{ticker}' not found" });
        }
        
        return Ok(asset);
    }

    /// <summary>
    /// Get asset detail with current price and user holding valuation
    /// </summary>
    [HttpGet("{assetId:guid}/detail")]
    [Authorize]
    [ProducesResponseType(typeof(AssetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAssetDetail(Guid assetId, CancellationToken ct)
    {
        try
        {
            var detail = await _getAssetDetailHandler.HandleAsync(new GetAssetDetailQuery(assetId), ct);
            if (detail is null)
                return NotFound(new { message = $"Asset with ID {assetId} not found" });

            return Ok(detail);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized asset detail access attempt");
            return Unauthorized(new { message = ex.Message });
        }
    }
}
