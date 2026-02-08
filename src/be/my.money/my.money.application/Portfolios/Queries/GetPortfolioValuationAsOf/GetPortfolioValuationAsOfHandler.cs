using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;

namespace my.money.application.Portfolios.Queries.GetPortfolioValuationAsOf;

public sealed class GetPortfolioValuationAsOfHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public GetPortfolioValuationAsOfHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<PortfolioValuationDto> HandleAsync(
        GetPortfolioValuationAsOfQuery query,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        var portfolio = await _unitOfWork.Portfolios.GetByUserIdWithHoldingsAsync(_currentUser.UserId, ct);

        if (portfolio is null)
            throw new InvalidOperationException("Portfolio not found for the authenticated user");

        // Get all asset IDs from holdings
        var assetIds = portfolio.Holdings.Select(h => h.AssetId).ToList();

        if (assetIds.Count == 0)
        {
            // Portfolio has no holdings, return valuation with only cash
            return new PortfolioValuationDto(
                AsOfUtc: query.AsOfUtc,
                CashBalanceAmount: portfolio.CashBalance.Amount,
                Currency: portfolio.CashBalance.Currency,
                Holdings: new List<PortfolioValuationLineDto>(),
                TotalHoldingsValue: 0m,
                TotalPortfolioValue: portfolio.CashBalance.Amount
            );
        }

        // Fetch quotes for all assets at or before the requested date in a single query
        // Using LINQ to group by AssetId and get the latest quote <= asOfUtc
        var quotesByAssetId = await _unitOfWork.Quotes
            .GetLatestQuotesAtOrBeforeAsync(assetIds, query.AsOfUtc, ct);

        var valuationLines = new List<PortfolioValuationLineDto>();
        var totalHoldingsValue = 0m;

        foreach (var holding in portfolio.Holdings)
        {
            var asset = await _unitOfWork.Assets.GetByIdAsync(holding.AssetId, ct);
            if (asset is null)
                continue; // Skip if asset not found (shouldn't happen in normal circumstances)

            if (!quotesByAssetId.TryGetValue(holding.AssetId, out var quote))
            {
                // No quote exists for this asset at or before asOfUtc
                throw new InvalidOperationException(
                    $"No historical quote found for asset '{asset.Ticker.Value}' at or before {query.AsOfUtc:O}. " +
                    $"Cannot calculate valuation for the requested date.");
            }

            var valuation = quote.Price.Amount * holding.Quantity.Value;
            totalHoldingsValue += valuation;

            valuationLines.Add(new PortfolioValuationLineDto(
                AssetId: holding.AssetId,
                Ticker: asset.Ticker.Value,
                Name: asset.Name,
                Quantity: holding.Quantity.Value,
                PriceAsOf: quote.Price.Amount,
                PriceAsOfUtc: quote.AsOfUtc,
                Valuation: valuation
            ));
        }

        var totalPortfolioValue = portfolio.CashBalance.Amount + totalHoldingsValue;

        return new PortfolioValuationDto(
            AsOfUtc: query.AsOfUtc,
            CashBalanceAmount: portfolio.CashBalance.Amount,
            Currency: portfolio.CashBalance.Currency,
            Holdings: valuationLines,
            TotalHoldingsValue: totalHoldingsValue,
            TotalPortfolioValue: totalPortfolioValue
        );
    }
}
