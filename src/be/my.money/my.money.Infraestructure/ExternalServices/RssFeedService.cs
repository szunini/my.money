using System.ServiceModel.Syndication;
using System.Xml;
using my.money.application.Ports.ExternalServices;
using Microsoft.Extensions.Logging;

namespace my.money.Infraestructure.ExternalServices;

public sealed class RssFeedService : IRssFeedService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssFeedService> _logger;

    public RssFeedService(HttpClient httpClient, ILogger<RssFeedService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<RssNewsItem>> FetchFeedAsync(string feedUrl, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching RSS feed from: {FeedUrl}", feedUrl);

            var response = await _httpClient.GetAsync(feedUrl, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = XmlReader.Create(stream);

            var feed = SyndicationFeed.Load(reader);
            var items = new List<RssNewsItem>();

            foreach (var item in feed.Items)
            {
                items.Add(new RssNewsItem
                {
                    Title = item.Title?.Text ?? "No Title",
                    Url = item.Links.FirstOrDefault()?.Uri.AbsoluteUri ?? item.Id,
                    PublishedAtUtc = item.PublishDate.UtcDateTime,
                    Summary = item.Summary?.Text ?? string.Empty
                });
            }

            _logger.LogInformation("Successfully fetched {ItemCount} items from {FeedUrl}", items.Count, feedUrl);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching RSS feed from {FeedUrl}", feedUrl);
            throw;
        }
    }
}
