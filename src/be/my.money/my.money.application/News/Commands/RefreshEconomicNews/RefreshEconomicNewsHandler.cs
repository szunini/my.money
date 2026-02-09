using my.money.application.Ports.ExternalServices;
using my.money.application.Ports.Persistence;
using my.money.application.Ports.Queries;
using my.money.domain.Aggregates.News;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace my.money.application.News.Commands.RefreshEconomicNews;

public sealed class RefreshEconomicNewsHandler
{
    private readonly IRssFeedService _rssFeedService;
    private readonly IOpenAiService _openAiService;
    private readonly INewsItemRepository _newsItemRepository;
    private readonly INewsMentionRepository _newsMentionRepository;
    private readonly IAssetQueryService _assetQueryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshEconomicNewsHandler> _logger;

    public RefreshEconomicNewsHandler(
        IRssFeedService rssFeedService,
        IOpenAiService openAiService,
        INewsItemRepository newsItemRepository,
        INewsMentionRepository newsMentionRepository,
        IAssetQueryService assetQueryService,
        IUnitOfWork unitOfWork,
        ILogger<RefreshEconomicNewsHandler> logger)
    {
        _rssFeedService = rssFeedService;
        _openAiService = openAiService;
        _newsItemRepository = newsItemRepository;
        _newsMentionRepository = newsMentionRepository;
        _assetQueryService = assetQueryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(RefreshEconomicNewsCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Starting economic news refresh with confidence threshold: {Threshold}", command.ConfidenceThreshold);
        try
        {
            // 1. Fetch ALL RSS items from ALL configured feeds (no DB check yet)
            var feeds = new List<(string url, string key)>
            {
                ("https://es.investing.com/rss/news_301.rss", "Investing 301"),
                ("https://www.ambito.com/rss/pages/finanzas.xml", "Ambito Finanzas"),
                ("https://www.ambito.com/rss/pages/ultimas-noticias.xml", "Ambito Ultimas")
            };

            var allItems = new List<NewsItem>();
            foreach (var feed in feeds)
            {
                var items = await _rssFeedService.FetchFeedAsync(feed.url, ct);
                foreach (var item in items)
                {
                    // Tag NewsItem with its feed/source key
                    var newsItem = new NewsItem(feed.key, item.Title, item.Url, item.PublishedAtUtc, item.Summary);
                    allItems.Add(newsItem);
                }
            }

            // 2. Deduplicate by NewsItem.Url
            var uniqueItems = allItems
                .GroupBy(x => x.Url)
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Fetched {UniqueCount} unique news items from all feeds", uniqueItems.Count);

            // 3. Check repository for existing items (batch, avoid N+1)
            var urls = uniqueItems.Select(x => x.Url).ToList();
            // NOTE: Implement GetByUrlsAsync in your repository for efficient batch lookup if not present
            var existingItems = await _newsItemRepository.GetByUrlsAsync(urls, ct);
            var existingUrls = new HashSet<string>(existingItems.Select(x => x.Url));

            var newItems = uniqueItems
                .Where(x => !existingUrls.Contains(x.Url))
                .ToList();

            _logger.LogInformation("Found {NewItemsCount} new news items", newItems.Count);

            // Add new items to repository
            foreach (var newsItem in newItems)
            {
                _newsItemRepository.Add(newsItem);
            }

            // 4. Load all available assets
            var allAssets = await _assetQueryService.GetAllAssetsAsync(ct);
            var candidates = allAssets
                .Select(a => new AssetCandidateDto
                {
                    AssetId = a.Id,
                    Ticker = a.Ticker,
                    Name = a.Name
                })
                .ToList();

            _logger.LogInformation("Loaded {AssetCount} available assets for matching", candidates.Count);

            // 5. For each new item, detect mentions
            foreach (var newsItem in newItems)
            {
                try
                {
                    var keywordMatches = DetectKeywordMatches(newsItem.Title, newsItem.Summary, candidates);

                    if (keywordMatches.Count == 0)
                    {
                        _logger.LogDebug("No keyword matches found for news item: {Title}", newsItem.Title);
                        continue;
                    }

                    _logger.LogDebug("Found {MatchCount} keyword candidates for news item: {Title}",
                        keywordMatches.Count, newsItem.Title);

                    // Only call OpenAI if we have candidates
                    AnalyzeNewsResponse aiResponse;
                    try
                    {
                        aiResponse = await _openAiService.AnalyzeNewsAsync(
                            $"{newsItem.Title}\n\n{newsItem.Summary}",
                            keywordMatches,
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "OpenAI analysis failed for news item: {Title}. Using keyword matching fallback.", newsItem.Title);

                        // Fallback: use keyword matches with lower confidence
                        foreach (var candidate in keywordMatches)
                        {
                            var mention = new NewsMention(
                                newsItem.Id,
                                candidate.AssetId,
                                0.40m,
                                "Keyword match (no AI confirmation)",
                                candidate.Ticker);
                            _newsMentionRepository.Add(mention);
                        }
                        continue;
                    }

                    // 6. Store mentions above threshold
                    foreach (var result in aiResponse.Mentions)
                    {
                        if (result.Confidence >= command.ConfidenceThreshold)
                        {
                            var candidate = keywordMatches.FirstOrDefault(c => c.Ticker == result.Ticker);
                            if (candidate != null)
                            {
                                var mention = new NewsMention(
                                    newsItem.Id,
                                    candidate.AssetId,
                                    result.Confidence,
                                    result.Explanation,
                                    result.MatchedText ?? result.Ticker);

                                _newsMentionRepository.Add(mention);
                                _logger.LogInformation("Stored mention for asset {Ticker} with confidence {Confidence}",
                                    result.Ticker, result.Confidence);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing news item: {Title}", newsItem.Title);
                }
            }

            // 7. Save all changes
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during economic news refresh");
        }
        _logger.LogInformation("Economic news refresh completed successfully");
    }

    private List<AssetCandidateDto> DetectKeywordMatches(
        string title,
        string? summary,
        List<AssetCandidateDto> candidates)
    {
        var text = $"{title} {summary}".ToUpperInvariant();
        var matches = new List<AssetCandidateDto>();

        foreach (var candidate in candidates)
        {
            var ticker = candidate.Ticker.ToUpperInvariant();
            var name = candidate.Name.ToUpperInvariant();

            // Match ticker with word boundaries
            if (MatchWordBoundary(text, ticker))
            {
                if (!matches.Any(m => m.AssetId == candidate.AssetId))
                    matches.Add(candidate);
                continue;
            }

            // Match name tokens (e.g., "Banco" + "Galicia" = "Banco Galicia")
            var nameTokens = name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameTokens.Length > 0 && nameTokens.All(token => MatchWordBoundary(text, token)))
            {
                if (!matches.Any(m => m.AssetId == candidate.AssetId))
                    matches.Add(candidate);
            }
        }

        return matches;
    }

    private bool MatchWordBoundary(string text, string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return false;

        var pattern = $@"\b{Regex.Escape(word)}\b";
        return Regex.IsMatch(text, pattern);
    }
}
