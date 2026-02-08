using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.News;
using my.money.Infraestructure.Persistence;

namespace my.money.Infraestructure.Repositories;

public sealed class NewsMentionRepository : INewsMentionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NewsMentionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<NewsItem>> GetNewsForAssetAsync(Guid assetId, CancellationToken ct)
    {
        return await _dbContext.NewsItems
            .Where(n => n.Mentions.Any(m => m.AssetId == assetId))
            .OrderByDescending(n => n.PublishedAtUtc)
            .ToListAsync(ct);
    }

    public void Add(NewsMention mention)
    {
        _dbContext.NewsMentions.Add(mention);
    }
}
