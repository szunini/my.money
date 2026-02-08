using Microsoft.EntityFrameworkCore;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.News;
using my.money.Infraestructure.Persistence;

namespace my.money.Infraestructure.Repositories;

public sealed class NewsItemRepository : INewsItemRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NewsItemRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NewsItem?> GetByUrlAsync(string url, CancellationToken ct)
    {
        return await _dbContext.NewsItems
            .FirstOrDefaultAsync(n => n.Url == url, ct);
    }

    public async Task<IEnumerable<NewsItem>> GetRecentItemsAsync(int maxDays = 30, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-maxDays);
        return await _dbContext.NewsItems
            .Where(n => n.CreatedAtUtc >= cutoffDate)
            .OrderByDescending(n => n.PublishedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<NewsItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.NewsItems
            .FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public void Add(NewsItem newsItem)
    {
        _dbContext.NewsItems.Add(newsItem);
    }

    public void Update(NewsItem newsItem)
    {
        _dbContext.NewsItems.Update(newsItem);
    }
}
