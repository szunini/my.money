using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Assets;

namespace my.money.Infraestructure.Persistence.Repositories;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly ApplicationDbContext _dbContext;

    public QuoteRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<Guid, Quote>> GetLatestQuotesAtOrBeforeAsync(
        List<Guid> assetIds,
        DateTime asOfUtc,
        CancellationToken ct = default)
    {
        if (assetIds.Count == 0)
            return new Dictionary<Guid, Quote>();

        // Ensure asOfUtc is UTC
        var utcAsOf = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);

        // Query: For each asset, get the latest quote where AsOfUtc <= asofUtc
        var result = await _dbContext.Quotes
            .Where(q => assetIds.Contains(q.AssetId) && q.AsOfUtc <= utcAsOf)
            .GroupBy(q => q.AssetId)
            .Select(g => new
            {
                AssetId = g.Key,
                Quote = g.OrderByDescending(q => q.AsOfUtc).FirstOrDefault()
            })
            .Where(x => x.Quote != null)
            .ToDictionaryAsync(x => x.AssetId, x => x.Quote!, ct);

        return result;
    }
}
