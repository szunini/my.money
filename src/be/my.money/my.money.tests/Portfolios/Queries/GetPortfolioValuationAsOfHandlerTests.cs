using Xunit;
using my.money.application.Portfolios.Dtos;
using my.money.application.Portfolios.Queries.GetPortfolioValuationAsOf;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;

namespace my.money.Tests.Portfolios.Queries;

public class GetPortfolioValuationAsOfHandlerTests
{
    [Fact]
    public async Task HandleAsync_ThrowsUnauthorizedAccessException_WhenUserNotAuthenticated()
    {
        // Arrange
        var fakeCurrentUser = new FakeCurrentUser(null, false);
        var fakeUnitOfWork = new FakeUnitOfWork();
        var handler = new GetPortfolioValuationAsOfHandler(fakeCurrentUser, fakeUnitOfWork);
        var query = new GetPortfolioValuationAsOfQuery(DateTime.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_ThrowsInvalidOperationException_WhenPortfolioNotFound()
    {
        // Arrange
        var userId = "test-user-id";
        var fakeCurrentUser = new FakeCurrentUser(userId, true);
        var fakeUnitOfWork = new FakeUnitOfWork { PortfolioToReturn = null };
        var handler = new GetPortfolioValuationAsOfHandler(fakeCurrentUser, fakeUnitOfWork);
        var query = new GetPortfolioValuationAsOfQuery(DateTime.UtcNow);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(query));
        Assert.Contains("Portfolio not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ReturnsValuationWithOnlyCash_WhenPortfolioHasNoHoldings()
    {
        // Arrange
        var userId = "test-user-id";
        var asOfDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var portfolio = new Portfolio(userId, Money.Of(1_000_000m, "ARS"));

        var fakeCurrentUser = new FakeCurrentUser(userId, true);
        var fakeUnitOfWork = new FakeUnitOfWork 
        { 
            PortfolioToReturn = portfolio
        };
        var handler = new GetPortfolioValuationAsOfHandler(fakeCurrentUser, fakeUnitOfWork);
        var query = new GetPortfolioValuationAsOfQuery(asOfDate);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(asOfDate, result.AsOfUtc);
        Assert.Equal(1_000_000m, result.CashBalanceAmount);
        Assert.Equal("ARS", result.Currency);
        Assert.Empty(result.Holdings);
        Assert.Equal(0m, result.TotalHoldingsValue);
        Assert.Equal(1_000_000m, result.TotalPortfolioValue);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectValuation_WhenQuotesExistAtOrBeforeDate()
    {
        // Arrange
        var userId = "test-user-id";
        var assetId = Guid.NewGuid();
        var asOfDate = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var quoteDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

        // Create asset
        var asset = new Asset(
            new Ticker("AAPL"),
            "Apple Inc",
            AssetType.Stock,
            "ARS");

        // Create portfolio with holding
        var portfolio = new Portfolio(userId, Money.Of(500_000m, "ARS"));
        portfolio.Buy(assetId, "ARS", Quantity.Of(100m), Money.Of(1200m, "ARS"));

        // Create quote - we'll need to create it through the asset
        asset.AddQuote(Money.Of(1200m, "ARS"), quoteDate, "manual");

        var fakeCurrentUser = new FakeCurrentUser(userId, true);
        var fakeUnitOfWork = new FakeUnitOfWork 
        { 
            PortfolioToReturn = portfolio,
            AssetToReturn = asset,
            QuotesToReturn = new Dictionary<Guid, Quote>
            {
                { assetId, asset.GetLatestQuoteAtOrBefore(asOfDate)! }
            }
        };
        var handler = new GetPortfolioValuationAsOfHandler(fakeCurrentUser, fakeUnitOfWork);
        var query = new GetPortfolioValuationAsOfQuery(asOfDate);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(asOfDate, result.AsOfUtc);
        Assert.Equal(380_000m, result.CashBalanceAmount); // 500,000 - 120,000 (100 * 1200)
        Assert.Equal("ARS", result.Currency);
        Assert.Single(result.Holdings);
        
        var line = result.Holdings[0];
        Assert.Equal(assetId, line.AssetId);
        Assert.Equal("AAPL", line.Ticker);
        Assert.Equal(100m, line.Quantity);
        Assert.Equal(1200m, line.PriceAsOf);
        Assert.Equal(120_000m, line.Valuation);
        Assert.Equal(120_000m, result.TotalHoldingsValue);
        Assert.Equal(500_000m, result.TotalPortfolioValue); // 380,000 cash + 120,000 holdings
    }

    [Fact]
    public async Task HandleAsync_ThrowsInvalidOperationException_WhenQuoteMissingForAsset()
    {
        // Arrange
        var userId = "test-user-id";
        var assetId = Guid.NewGuid();
        var asOfDate = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create asset without quotes
        var asset = new Asset(
            new Ticker("TSLA"),
            "Tesla Inc",
            AssetType.Stock,
            "ARS");

        // Create portfolio with holding
        var portfolio = new Portfolio(userId, Money.Of(500_000m, "ARS"));
        portfolio.Buy(assetId, "ARS", Quantity.Of(50m), Money.Of(800m, "ARS"));

        // No quotes available
        var fakeCurrentUser = new FakeCurrentUser(userId, true);
        var fakeUnitOfWork = new FakeUnitOfWork 
        { 
            PortfolioToReturn = portfolio,
            AssetToReturn = asset,
            QuotesToReturn = new Dictionary<Guid, Quote>() // Empty
        };
        var handler = new GetPortfolioValuationAsOfHandler(fakeCurrentUser, fakeUnitOfWork);
        var query = new GetPortfolioValuationAsOfQuery(asOfDate);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(query));
        Assert.Contains("No historical quote found", ex.Message);
        Assert.Contains("TSLA", ex.Message);
    }
}

// Fake implementations
file sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(string? userId, bool isAuthenticated)
    {
        UserId = userId;
        IsAuthenticated = isAuthenticated;
    }

    public string? UserId { get; }
    public bool IsAuthenticated { get; }
}

file sealed class FakeUnitOfWork : IUnitOfWork
{
    public Portfolio? PortfolioToReturn { get; set; }
    public Asset? AssetToReturn { get; set; }
    public Dictionary<Guid, Quote> QuotesToReturn { get; set; } = new();

    public IPortfolioRepository Portfolios => new FakePortfolioRepository(this);
    public IAssetRepository Assets => new FakeAssetRepository(this);
    public IQuoteRepository Quotes => new FakeQuoteRepository(this);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync() => Task.CompletedTask;
    public Task CommitTransactionAsync() => Task.CompletedTask;
    public Task RollbackTransactionAsync() => Task.CompletedTask;
    public void Dispose() { }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakePortfolioRepository(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(_unitOfWork.PortfolioToReturn);

        public Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken ct)
            => Task.FromResult(_unitOfWork.PortfolioToReturn);

        public Task<Portfolio?> GetByUserIdWithHoldingsAsync(string userId, CancellationToken ct)
            => Task.FromResult(_unitOfWork.PortfolioToReturn);

        public Task AddAsync(Portfolio portfolio, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAssetRepository : IAssetRepository
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakeAssetRepository(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(_unitOfWork.AssetToReturn);

        public Task<Asset?> GetByTickerAsync(string ticker, CancellationToken ct)
            => Task.FromResult(_unitOfWork.AssetToReturn);

        public Task<IReadOnlyList<Asset>> ListAllAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Asset>>(Array.Empty<Asset>());

        public Task AddAsync(Asset asset, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeQuoteRepository : IQuoteRepository
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakeQuoteRepository(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Dictionary<Guid, Quote>> GetLatestQuotesAtOrBeforeAsync(
            List<Guid> assetIds,
            DateTime asofUtc,
            CancellationToken ct = default)
        {
            return Task.FromResult(_unitOfWork.QuotesToReturn);
        }
    }
}
