using my.money.application.News.Dtos;
using my.money.application.Ports.Persistence;
using Microsoft.Extensions.Logging;

namespace my.money.application.News.Queries.GetAssetNews;

public sealed class GetAssetNewsHandler
{
    private readonly INewsMentionRepository _newsMentionRepository;
    private readonly ILogger<GetAssetNewsHandler> _logger;

    public GetAssetNewsHandler(
        INewsMentionRepository newsMentionRepository,
        ILogger<GetAssetNewsHandler> logger)
    {
        _newsMentionRepository = newsMentionRepository;
        _logger = logger;
    }

    public async Task<List<AssetMentionDto>> HandleAsync(GetAssetNewsQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Retrieving news for asset {AssetId}", query.AssetId);

        var newsItems = await _newsMentionRepository.GetNewsForAssetAsync(query.AssetId, ct);

        var mentions = newsItems
            .SelectMany(n => n.Mentions
                .Where(m => m.AssetId == query.AssetId)
                .OrderByDescending(m => m.DetectedAtUtc)
                .Select(m => new AssetMentionDto
                {
                    NewsItemId = n.Id,
                    Source = n.Source,
                    Title = n.Title,
                    Url = n.Url,
                    PublishedAtUtc = n.PublishedAtUtc,
                    Summary = n.Summary,
                    Confidence = m.Confidence,
                    Explanation = m.Explanation,
                    MatchedText = m.MatchedText,
                    DetectedAtUtc = m.DetectedAtUtc
                }))
            .OrderByDescending(m => m.Confidence)
            .ThenByDescending(m => m.DetectedAtUtc)
            .ToList();

        _logger.LogInformation("Found {MentionCount} mentions for asset {AssetId}", mentions.Count, query.AssetId);

        return mentions;
    }
}
