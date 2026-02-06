using my.money.domain.Aggregates.Assets;

namespace my.money.application.Ports.Persistence
{
    public interface IAssetRepository
    {
        Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Asset?> GetByTickerAsync(string ticker, CancellationToken ct);
        Task<IReadOnlyList<Asset>> ListAllAsync(CancellationToken ct);
        Task AddAsync(Asset asset, CancellationToken ct);
    }
}
