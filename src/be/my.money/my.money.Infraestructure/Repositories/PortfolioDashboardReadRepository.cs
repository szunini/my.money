using Microsoft.EntityFrameworkCore;
using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Persistence.Read;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;
using my.money.Infraestructure.Persistence;

namespace my.money.Infraestructure.Repositories;

public sealed class PortfolioDashboardReadRepository : IPortfolioDashboardReadRepository
{
    private readonly ApplicationDbContext _context;

    public PortfolioDashboardReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardValuationDto> GetDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Get or create portfolio
        var portfolio = await _context.Set<Portfolio>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (portfolio is null)
        {
            // Lazy-create: portfolio doesn't exist yet
            portfolio = new Portfolio(userId, Money.Zero("ARS"));
            _context.Add(portfolio);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var cashBalance = portfolio.CashBalance.Amount;

        // Get holdings with asset details
        var holdingsWithAssets = await _context.Set<Holding>()
            .AsNoTracking()
            .Where(h => h.PortfolioId == portfolio.Id && h.Quantity.Value > 0)
            .Join(
                _context.Set<Asset>().AsNoTracking(),
                h => h.AssetId,
                a => a.Id,
                (h, a) => new
                {
                    Holding = h,
                    Asset = a
                })
            .ToListAsync(cancellationToken);

        // Get all asset IDs for later quote lookup
        var assetIds = holdingsWithAssets.Select(ha => ha.Asset.Id).Distinct().ToList();

        // Get latest quotes per asset (for holdings)
        var latestQuotesMap = await GetLatestQuotesAsync(assetIds, cancellationToken);

        // Build holding valuation items
        var holdingValuationItems = holdingsWithAssets
            .Select(ha =>
            {
                var quantity = ha.Holding.Quantity.Value;
                decimal? latestPrice = null;
                DateTime? latestPriceAsOfUtc = null;
                decimal? holdingValue = null;

                if (latestQuotesMap.TryGetValue(ha.Asset.Id, out var quote))
                {
                    latestPrice = quote.Price;
                    latestPriceAsOfUtc = quote.AsOfUtc;
                    holdingValue = quantity * latestPrice.Value;
                }

                return new HoldingValuationItemDto(
                    ha.Asset.Id,
                    ha.Asset.Ticker.Value,
                    ha.Asset.Name,
                    ha.Asset.Type.ToString(),
                    quantity,
                    latestPrice,
                    latestPriceAsOfUtc,
                    holdingValue
                );
            })
            .ToList();

        // Calculate total holdings value
        var totalHoldingsValue = holdingValuationItems
            .Where(h => h.HoldingValue.HasValue)
            .Sum(h => h.HoldingValue.Value);

        // Get all tradable assets with latest quotes
        var allAssets = await _context.Set<Asset>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allAssetIds = allAssets.Select(a => a.Id).ToList();
        var allLatestQuotesMap = await GetLatestQuotesAsync(allAssetIds, cancellationToken);

        var tradableAssets = allAssets
            .Select(a =>
            {
                decimal? latestPrice = null;
                DateTime? latestPriceAsOfUtc = null;

                if (allLatestQuotesMap.TryGetValue(a.Id, out var quote))
                {
                    latestPrice = quote.Price;
                    latestPriceAsOfUtc = quote.AsOfUtc;
                }

                return new TradableAssetDto(
                    a.Id,
                    a.Ticker.Value,
                    a.Name,
                    a.Type.ToString(),
                    latestPrice,
                    latestPriceAsOfUtc
                );
            })
            .ToList();

        var totalPortfolioValue = cashBalance + totalHoldingsValue;

        return new DashboardValuationDto(
            cashBalance,
            totalHoldingsValue,
            totalPortfolioValue,
            holdingValuationItems.AsReadOnly(),
            tradableAssets.AsReadOnly()
        );
    }

    private async Task<Dictionary<Guid, (decimal Price, DateTime AsOfUtc)>> GetLatestQuotesAsync(
        List<Guid> assetIds,
        CancellationToken cancellationToken)
    {
        if (!assetIds.Any())
            return new Dictionary<Guid, (decimal Price, DateTime AsOfUtc)>();

        // Get latest quote per asset (with max AsOfUtc)
        var latestQuotes = await _context.Set<Quote>()
            .AsNoTracking()
            .Where(q => assetIds.Contains(q.AssetId))
            .GroupBy(q => q.AssetId)
            .Select(g => new
            {
                AssetId = g.Key,
                LatestQuote = g.OrderByDescending(q => q.AsOfUtc).First()
            })
            .ToListAsync(cancellationToken);

        return latestQuotes.ToDictionary(
            x => x.AssetId,
            x => (x.LatestQuote.Price.Amount, x.LatestQuote.AsOfUtc)
        );
    }
}
