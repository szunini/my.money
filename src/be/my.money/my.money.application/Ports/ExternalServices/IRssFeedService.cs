namespace my.money.application.Ports.ExternalServices;

public interface IRssFeedService
{
    Task<List<RssNewsItem>> FetchFeedAsync(string feedUrl, CancellationToken ct);
}

public sealed class RssNewsItem
{
    public string Title { get; set; } = default!;
    public string Url { get; set; } = default!;
    public DateTime? PublishedAtUtc { get; set; }
    public string? Summary { get; set; }
}
