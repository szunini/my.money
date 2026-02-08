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

        // 1. Fetch RSS feeds
        var cronistItems = await _rssFeedService.FetchFeedAsync(
            "https://www.cronista.com/files/rss/negocios.xml", ct);
        var infobaeItems = await _rssFeedService.FetchFeedAsync(
            "https://www.infobae.com/feeds/rss/economia.xml", ct);

        _logger.LogInformation("Fetched {CronistCount} Cronista items and {InfobaeCount} Infobae items",
            cronistItems.Count, infobaeItems.Count);

        // 2. Normalize and store news items
        var newItems = new List<NewsItem>();

        foreach (var item in cronistItems)
        {
            var existing = await _newsItemRepository.GetByUrlAsync(item.Url, ct);
            if (existing == null)
            {
                var newsItem = new NewsItem("Cronista", item.Title, item.Url, item.PublishedAtUtc, item.Summary);
                _newsItemRepository.Add(newsItem);
                newItems.Add(newsItem);
            }
        }

        foreach (var item in infobaeItems)
        {
            var existing = await _newsItemRepository.GetByUrlAsync(item.Url, ct);
            if (existing == null)
            {
                var newsItem = new NewsItem("Infobae", item.Title, item.Url, item.PublishedAtUtc, item.Summary);
                _newsItemRepository.Add(newsItem);
                newItems.Add(newsItem);
            }
        }

        _logger.LogInformation("Found {NewItemsCount} new news items", newItems.Count);

        // 3. Load all available assets
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

        // 4. For each new item, detect mentions
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

                // 5. Store mentions above threshold
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

        // 6. Save all changes
        await _unitOfWork.SaveChangesAsync(ct);
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
