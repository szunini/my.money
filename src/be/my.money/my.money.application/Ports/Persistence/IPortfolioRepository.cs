using my.money.domain.Aggregates.Portfolios;

namespace my.money.application.Ports.Persistence
{
    public interface IPortfolioRepository
    {
        Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken ct);
        Task<Portfolio?> GetByUserIdWithHoldingsAsync(string userId, CancellationToken ct);
        Task AddAsync(Portfolio portfolio, CancellationToken ct);
    }
}
