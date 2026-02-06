using my.money.domain.aggregates;

namespace my.money.application.Ports.Persistence;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
