using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence.Read;

namespace my.money.application.Portfolios.Queries.GetDashboardValuation;

public sealed class GetDashboardValuationHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPortfolioDashboardReadRepository _dashboardReadRepository;

    public GetDashboardValuationHandler(
        ICurrentUser currentUser,
        IPortfolioDashboardReadRepository dashboardReadRepository)
    {
        _currentUser = currentUser;
        _dashboardReadRepository = dashboardReadRepository;
    }

    public async Task<DashboardValuationDto> HandleAsync(
        GetDashboardValuationQuery query,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        var dashboard = await _dashboardReadRepository.GetDashboardAsync(_currentUser.UserId, ct);

        return dashboard;
    }
}
