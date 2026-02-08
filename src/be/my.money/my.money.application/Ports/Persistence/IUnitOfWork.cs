namespace my.money.application.Ports.Persistence;

public interface IUnitOfWork : IDisposable
{
    IPortfolioRepository Portfolios { get; }
    IAssetRepository Assets { get; }
    IQuoteRepository Quotes { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

