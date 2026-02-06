using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.application.Portfolios.Queries.GetDashboard;

public sealed class GetDashboardHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDto> HandleAsync(GetDashboardQuery query, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        // Get or create portfolio
        var portfolio = await _unitOfWork.Portfolios.GetByUserIdAsync(_currentUser.UserId, ct);
        
        if (portfolio is null)
        {
            // Lazy-create: portfolio doesn't exist yet, create with 0 ARS
            portfolio = new Portfolio(_currentUser.UserId, Money.Zero("ARS"));
            await _unitOfWork.Portfolios.AddAsync(portfolio, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Get all available assets
        var allAssets = await _unitOfWork.Assets.ListAllAsync(ct);

        // Build holdings DTOs
        var holdingDtos = portfolio.Holdings
            .Where(h => h.Quantity.Value > 0) // Only show holdings with quantity > 0
            .Select(h =>
            {
                var asset = allAssets.FirstOrDefault(a => a.Id == h.AssetId);
                return new HoldingItemDto(
                    h.AssetId,
                    asset?.Ticker.Value ?? "UNKNOWN",
                    asset?.Name ?? "Unknown Asset",
                    asset?.Type.ToString() ?? "Unknown",
                    h.Quantity.Value
                );
            })
            .ToList();

        // Build available assets DTOs
        var assetDtos = allAssets
            .Select(a => new AssetItemDto(
                a.Id,
                a.Ticker.Value,
                a.Name,
                a.Type.ToString()
            ))
            .ToList();

        return new DashboardDto(
            portfolio.CashBalance.Amount,
            holdingDtos,
            assetDtos
        );
    }
}
