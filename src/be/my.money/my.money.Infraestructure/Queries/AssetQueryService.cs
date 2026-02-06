using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Queries;
using my.money.Infraestructure.Persistence;

namespace my.money.Infraestructure.Queries;

public sealed class AssetQueryService : IAssetQueryService
{
    private readonly ApplicationDbContext _db;

    public AssetQueryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<AssetDto>> GetAllAssetsAsync(CancellationToken ct = default)
    {
        return await _db.Assets
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Ticker.Value)
            .Select(a => new AssetDto(
                a.Id,
                a.Ticker.Value,
                a.Name,
                a.Type.ToString(),
                a.Currency
            ))
            .ToListAsync(ct);
    }

    public async Task<AssetDto?> GetAssetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Assets
            .Where(a => a.Id == id)
            .Select(a => new AssetDto(
                a.Id,
                a.Ticker.Value,
                a.Name,
                a.Type.ToString(),
                a.Currency
            ))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<AssetDto?> GetAssetByTickerAsync(string ticker, CancellationToken ct = default)
    {
        var normalizedTicker = ticker.Trim().ToUpperInvariant();
        
        return await _db.Assets
            .Where(a => a.Ticker.Value == normalizedTicker)
            .Select(a => new AssetDto(
                a.Id,
                a.Ticker.Value,
                a.Name,
                a.Type.ToString(),
                a.Currency
            ))
            .SingleOrDefaultAsync(ct);
    }
}
