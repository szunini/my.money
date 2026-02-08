using my.money.application.Portfolios.Commands.BuyAsset;
using my.money.application.Portfolios.Commands.SellAsset;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;
using Xunit;

namespace my.money.tests.Portfolios;

public sealed class BuyAssetHandlerTests
{
    [Fact]
    public async Task BuyAsset_ThrowsInsufficientCashException_WhenNotEnoughCash()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(100000m, "ARS"), DateTime.UtcNow, "test");
        
        var portfolio = new Portfolio(userId, Money.Of(1000m, "ARS")); // Only 1000 ARS
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(portfolio, asset);
        
        var handler = new BuyAssetHandler(currentUser, unitOfWork);
        var command = new BuyAssetCommand(assetId, 10m); // Trying to buy 10 units at 100000 each = 1,000,000
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command)
        );
        
        Assert.Contains("Insufficient cash", exception.Message);
        Assert.True(unitOfWork.TransactionRolledBack);
    }

    [Fact]
    public async Task BuyAsset_Succeeds_WhenSufficientCash()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(1000m, "ARS"), DateTime.UtcNow, "test");
        
        var portfolio = new Portfolio(userId, Money.Of(100000m, "ARS")); // 100,000 ARS
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(portfolio, asset);
        
        var handler = new BuyAssetHandler(currentUser, unitOfWork);
        var command = new BuyAssetCommand(assetId, 10m); // Buying 10 units at 1000 each = 10,000
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(assetId, result.AssetId);
        Assert.Equal("Buy", result.Side);
        Assert.Equal(10m, result.Quantity);
        Assert.Equal(1000m, result.Price);
        Assert.Equal(10000m, result.TotalAmount);
        Assert.Equal(90000m, result.RemainingCash); // 100,000 - 10,000
        Assert.True(unitOfWork.TransactionCommitted);
    }

    [Fact]
    public async Task BuyAsset_CreatesPortfolio_WhenNoneExists()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(1000m, "ARS"), DateTime.UtcNow, "test");
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(null, asset); // No portfolio
        
        var handler = new BuyAssetHandler(currentUser, unitOfWork);
        var command = new BuyAssetCommand(assetId, 10m);
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(unitOfWork.PortfolioWasCreated);
        Assert.Equal(990000m, result.RemainingCash); // 1,000,000 initial - 10,000
    }
}

public sealed class SellAssetHandlerTests
{
    [Fact]
    public async Task SellAsset_ThrowsInsufficientQuantityException_WhenNotEnoughHoldings()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(1000m, "ARS"), DateTime.UtcNow, "test");
        
        var portfolio = new Portfolio(userId, Money.Of(100000m, "ARS"));
        // Buy only 5 units
        portfolio.Buy(assetId, "ARS", Quantity.Of(5m), Money.Of(1000m, "ARS"));
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(portfolio, asset);
        
        var handler = new SellAssetHandler(currentUser, unitOfWork);
        var command = new SellAssetCommand(assetId, 10m); // Trying to sell 10 but only have 5
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command)
        );
        
        Assert.Contains("Insufficient quantity", exception.Message);
        Assert.True(unitOfWork.TransactionRolledBack);
    }

    [Fact]
    public async Task SellAsset_Succeeds_WhenSufficientHoldings()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(1000m, "ARS"), DateTime.UtcNow, "test");
        
        var portfolio = new Portfolio(userId, Money.Of(100000m, "ARS"));
        // Buy 10 units first
        portfolio.Buy(assetId, "ARS", Quantity.Of(10m), Money.Of(1000m, "ARS"));
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(portfolio, asset);
        
        var handler = new SellAssetHandler(currentUser, unitOfWork);
        var command = new SellAssetCommand(assetId, 5m); // Sell 5 out of 10
        
        // Act
        var result = await handler.HandleAsync(command);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(assetId, result.AssetId);
        Assert.Equal("Sell", result.Side);
        Assert.Equal(5m, result.Quantity);
        Assert.Equal(1000m, result.Price);
        Assert.Equal(5000m, result.TotalAmount);
        Assert.Equal(95000m, result.RemainingCash); // 90,000 (after buy) + 5,000 (from sell)
        Assert.True(unitOfWork.TransactionCommitted);
    }

    [Fact]
    public async Task SellAsset_ThrowsException_WhenPortfolioNotFound()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var userId = "user-1";
        
        var asset = new Asset(
            Ticker.Of("AAPL"),
            "Apple Inc.",
            AssetType.Stock,
            "ARS"
        );
        asset.AddQuote(Money.Of(1000m, "ARS"), DateTime.UtcNow, "test");
        
        var currentUser = new FakeCurrentUser(userId);
        var unitOfWork = new FakeUnitOfWork(null, asset); // No portfolio
        
        var handler = new SellAssetHandler(currentUser, unitOfWork);
        var command = new SellAssetCommand(assetId, 5m);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command)
        );
        
        Assert.Contains("Portfolio not found", exception.Message);
    }
}

// Test Fakes
file sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(string userId)
    {
        UserId = userId;
    }

    public string? UserId { get; }
    public bool IsAuthenticated => true;
}

file sealed class FakeUnitOfWork : IUnitOfWork
{
    private Portfolio? _portfolio;
    private readonly Asset? _asset;
    private bool _transactionStarted;

    public bool TransactionCommitted { get; private set; }
    public bool TransactionRolledBack { get; private set; }
    public bool PortfolioWasCreated { get; private set; }

    public FakeUnitOfWork(Portfolio? portfolio, Asset? asset)
    {
        _portfolio = portfolio;
        _asset = asset;
    }

    public IPortfolioRepository Portfolios => new FakePortfolioRepository(this);
    public IAssetRepository Assets => new FakeAssetRepository(_asset);
    public IQuoteRepository Quotes => new FakeQuoteRepository();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync()
    {
        _transactionStarted = true;
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync()
    {
        if (!_transactionStarted)
            throw new InvalidOperationException("Transaction not started");
        TransactionCommitted = true;
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync()
    {
        if (!_transactionStarted)
            throw new InvalidOperationException("Transaction not started");
        TransactionRolledBack = true;
        return Task.CompletedTask;
    }

    public void Dispose() { }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakePortfolioRepository(FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(_unitOfWork._portfolio);
        }

        public Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken ct)
        {
            return Task.FromResult(_unitOfWork._portfolio);
        }

        public Task<Portfolio?> GetByUserIdWithHoldingsAsync(string userId, CancellationToken ct)
        {
            return Task.FromResult(_unitOfWork._portfolio);
        }

        public Task AddAsync(Portfolio portfolio, CancellationToken ct)
        {
            _unitOfWork._portfolio = portfolio;
            _unitOfWork.PortfolioWasCreated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAssetRepository : IAssetRepository
    {
        private readonly Asset? _asset;

        public FakeAssetRepository(Asset? asset)
        {
            _asset = asset;
        }

        public Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(_asset);
        }

        public Task<Asset?> GetByTickerAsync(string ticker, CancellationToken ct)
        {
            return Task.FromResult(_asset);
        }

        public Task<IReadOnlyList<Asset>> ListAllAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<Asset>>(Array.Empty<Asset>());
        }

        public Task AddAsync(Asset asset, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeQuoteRepository : IQuoteRepository
    {
        public Task<Dictionary<Guid, my.money.domain.Aggregates.Assets.Quote>> GetLatestQuotesAtOrBeforeAsync(
            List<Guid> assetIds,
            DateTime asofUtc,
            CancellationToken ct = default)
        {
            return Task.FromResult(new Dictionary<Guid, my.money.domain.Aggregates.Assets.Quote>());
        }
    }
}
