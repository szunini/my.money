using my.money.application.Assets.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence.Read;

namespace my.money.application.Assets.Queries.GetAssetDetail;

public sealed class GetAssetDetailHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IAssetDetailReadRepository _assetDetailReadRepository;

    public GetAssetDetailHandler(
        ICurrentUser currentUser,
        IAssetDetailReadRepository assetDetailReadRepository)
    {
        _currentUser = currentUser;
        _assetDetailReadRepository = assetDetailReadRepository;
    }

    public async Task<AssetDetailDto?> HandleAsync(GetAssetDetailQuery query, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        var detail = await _assetDetailReadRepository.GetAssetDetailAsync(query.AssetId, _currentUser.UserId, ct);
        if (detail is null)
            return null;

        var valuation = detail.QuantityOwned * detail.CurrentPrice;

        return new AssetDetailDto(
            detail.AssetId,
            detail.Symbol,
            detail.Name,
            detail.Type,
            detail.CurrentPrice,
            detail.QuantityOwned,
            valuation
        );
    }
}
