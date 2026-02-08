namespace my.money.application.News.Dtos;

public sealed class NewsItemDto
{
    public Guid Id { get; set; }
    public string Source { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Url { get; set; } = default!;
    public DateTime? PublishedAtUtc { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<NewsMentionDto> Mentions { get; set; } = new();
}

public sealed class NewsMentionDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = default!;
    public string? MatchedText { get; set; }
    public DateTime DetectedAtUtc { get; set; }
}

public sealed class AssetMentionDto
{
    public Guid NewsItemId { get; set; }
    public string Source { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Url { get; set; } = default!;
    public DateTime? PublishedAtUtc { get; set; }
    public string? Summary { get; set; }
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = default!;
    public string? MatchedText { get; set; }
    public DateTime DetectedAtUtc { get; set; }
}
