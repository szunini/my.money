using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Assets;

namespace my.money.Infraestructure.Persistence.Repositories
{
    public sealed class AssetRepository : IAssetRepository
    {
        private readonly ApplicationDbContext _db;

        public AssetRepository(ApplicationDbContext db) => _db = db;

        public Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct)
            => _db.Assets
                  .Include(a => a.Quotes)
                  .SingleOrDefaultAsync(a => a.Id == id, ct);

        public Task<Asset?> GetByTickerAsync(string ticker, CancellationToken ct)
            => _db.Assets
                  .Include(a => a.Quotes)
                  .SingleOrDefaultAsync(a => a.Ticker.Value == ticker.ToUpper(), ct);

        public async Task<IReadOnlyList<Asset>> ListAllAsync(CancellationToken ct)
            => await _db.Assets
                  .AsNoTracking()
                  .OrderBy(a => a.Type)
                  .ThenBy(a => a.Ticker.Value)
                  .ToListAsync(ct);

        public Task AddAsync(Asset asset, CancellationToken ct)
            => _db.Assets.AddAsync(asset, ct).AsTask();
    }
}
