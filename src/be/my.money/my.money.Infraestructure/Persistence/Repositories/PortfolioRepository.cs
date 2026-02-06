using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Portfolios;

namespace my.money.Infraestructure.Persistence.Repositories
{
    public sealed class PortfolioRepository : IPortfolioRepository
    {
        private readonly ApplicationDbContext _db;

        public PortfolioRepository(ApplicationDbContext db) => _db = db;

        public Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken ct)
            => _db.Portfolios
                  .Include(p => p.Holdings)
                  .Include(p => p.Trades)
                  .SingleOrDefaultAsync(p => p.Id == id, ct);

        public Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken ct)
            => _db.Portfolios
                  .Include(p => p.Holdings)
                  .Include(p => p.Trades)
                  .SingleOrDefaultAsync(p => p.UserId == userId, ct);

        public Task AddAsync(Portfolio portfolio, CancellationToken ct)
            => _db.Portfolios.AddAsync(portfolio, ct).AsTask();
    }
}
