using Microsoft.EntityFrameworkCore;
using my.money.application.Assets.Queries.GetAssetDetail;
using my.money.application.Ports.Persistence.Read;
using my.money.Infraestructure.Persistence;

namespace my.money.Infraestructure.Repositories;

public sealed class AssetDetailReadRepository : IAssetDetailReadRepository
{
    private readonly ApplicationDbContext _context;

    public AssetDetailReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssetDetailReadModel?> GetAssetDetailAsync(Guid assetId, string userId, CancellationToken cancellationToken = default)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => new
            {
                a.Id,
                Symbol = a.Ticker.Value,
                a.Name,
                Type = a.Type.ToString()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (asset is null)
            return null;

        var portfolioId = await _context.Portfolios
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var quantityOwned = 0m;
        if (portfolioId.HasValue)
        {

            var holdings = await _context.Holdings
                .AsNoTracking()
                .Where(h => h.PortfolioId == portfolioId.Value && h.AssetId == assetId)
                .Select(h => h.Quantity.Value)
                .ToListAsync(cancellationToken);

            quantityOwned = holdings.Any() ? holdings.Sum() : 0m;
        }

        var currentPrice = await _context.Quotes
            .AsNoTracking()
            .Where(q => q.AssetId == assetId)
            .OrderByDescending(q => q.AsOfUtc)
            .Select(q => (decimal?)q.Price.Amount)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        return new AssetDetailReadModel(
            asset.Id,
            asset.Symbol,
            asset.Name,
            asset.Type,
            currentPrice,
            quantityOwned
        );
    }
}
