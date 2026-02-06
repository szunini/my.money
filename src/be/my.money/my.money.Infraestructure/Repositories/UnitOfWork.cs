using Microsoft.EntityFrameworkCore.Storage;
using my.money.application.Ports.Persistence;
using my.money.Infraestructure.Persistence;
using my.money.Infraestructure.Persistence.Repositories;

namespace my.money.Infraestructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private IPortfolioRepository? _portfolios;
    private IAssetRepository? _assets;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IPortfolioRepository Portfolios
    {
        get
        {
            _portfolios ??= new PortfolioRepository(_context);
            return _portfolios;
        }
    }

    public IAssetRepository Assets
    {
        get
        {
            _assets ??= new AssetRepository(_context);
            return _assets;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
