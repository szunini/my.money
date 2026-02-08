using my.money.domain.Aggregates.News;

namespace my.money.application.Ports.Persistence;

public interface INewsItemRepository
{
    Task<NewsItem?> GetByUrlAsync(string url, CancellationToken ct);
    Task<IEnumerable<NewsItem>> GetRecentItemsAsync(int maxDays = 30, CancellationToken ct = default);
    Task<NewsItem?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(NewsItem newsItem);
    void Update(NewsItem newsItem);
}

public interface INewsMentionRepository
{
    Task<IEnumerable<NewsItem>> GetNewsForAssetAsync(Guid assetId, CancellationToken ct);
    void Add(NewsMention mention);
}
