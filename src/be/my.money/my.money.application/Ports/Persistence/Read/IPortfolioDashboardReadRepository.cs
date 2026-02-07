using my.money.application.Portfolios.Dtos;

namespace my.money.application.Ports.Persistence.Read;

public interface IPortfolioDashboardReadRepository
{
    Task<DashboardValuationDto> GetDashboardAsync(string userId, CancellationToken cancellationToken = default);
}
