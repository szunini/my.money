using my.money.application.Assets.Queries.GetAssetDetail;

namespace my.money.application.Ports.Persistence.Read;

public interface IAssetDetailReadRepository
{
    Task<AssetDetailReadModel?> GetAssetDetailAsync(Guid assetId, string userId, CancellationToken cancellationToken = default);
}
