using my.money.application.Assets.Queries.GetAssetDetail;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence.Read;

namespace my.money.tests.Assets;

public sealed class GetAssetDetailHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsValuationFromQuantityAndPrice()
    {
        var assetId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser("user-1");
        var repo = new FakeAssetDetailReadRepository(new AssetDetailReadModel(
            assetId,
            "ALUA",
            "Aluar",
            "Stock",
            125m,
            4m));

        var handler = new GetAssetDetailHandler(currentUser, repo);

        var result = await handler.HandleAsync(new GetAssetDetailQuery(assetId));

        Assert.NotNull(result);
        Assert.Equal(4m, result!.QuantityOwned);
        Assert.Equal(125m, result.CurrentPrice);
        Assert.Equal(500m, result.Valuation);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroValuationWhenQuantityIsZero()
    {
        var assetId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser("user-1");
        var repo = new FakeAssetDetailReadRepository(new AssetDetailReadModel(
            assetId,
            "ALUA",
            "Aluar",
            "Stock",
            125m,
            0m));

        var handler = new GetAssetDetailHandler(currentUser, repo);

        var result = await handler.HandleAsync(new GetAssetDetailQuery(assetId));

        Assert.NotNull(result);
        Assert.Equal(0m, result!.QuantityOwned);
        Assert.Equal(0m, result.Valuation);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(string userId)
        {
            UserId = userId;
        }

        public string? UserId { get; }
        public bool IsAuthenticated => true;
    }

    private sealed class FakeAssetDetailReadRepository : IAssetDetailReadRepository
    {
        private readonly AssetDetailReadModel? _detail;

        public FakeAssetDetailReadRepository(AssetDetailReadModel? detail)
        {
            _detail = detail;
        }

        public Task<AssetDetailReadModel?> GetAssetDetailAsync(Guid assetId, string userId, CancellationToken cancellationToken = default)
        {
            if (_detail is null || _detail.AssetId != assetId)
                return Task.FromResult<AssetDetailReadModel?>(null);

            return Task.FromResult<AssetDetailReadModel?>(_detail);
        }
    }
}
