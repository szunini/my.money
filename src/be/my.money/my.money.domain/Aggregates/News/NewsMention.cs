using my.money.domain.Common.Primitives;

namespace my.money.domain.Aggregates.News;

public sealed class NewsMention : Entity<Guid>
{
    public Guid NewsItemId { get; private set; }
    public Guid AssetId { get; private set; }
    public decimal Confidence { get; private set; }  // 0.00 to 1.00
    public string Explanation { get; private set; } = default!;
    public string? MatchedText { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }

    private NewsMention() { } // EF

    public NewsMention(Guid newsItemId, Guid assetId, decimal confidence, string explanation, string? matchedText = null)
    {
        if (confidence < 0 || confidence > 1)
            throw new ArgumentException("Confidence must be between 0 and 1", nameof(confidence));

        Id = Guid.NewGuid();
        NewsItemId = newsItemId;
        AssetId = assetId;
        Confidence = confidence;
        Explanation = explanation;
        MatchedText = matchedText;
        DetectedAtUtc = DateTime.UtcNow;
    }
}
