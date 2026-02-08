namespace my.money.application.Ports.ExternalServices;

public interface IOpenAiService
{
    Task<AnalyzeNewsResponse> AnalyzeNewsAsync(
        string articleText, 
        IEnumerable<AssetCandidateDto> candidates,
        CancellationToken ct);
}

public sealed class AnalyzeNewsResponse
{
    public List<MentionResult> Mentions { get; set; } = new();
}

public sealed class MentionResult
{
    public string Ticker { get; set; } = default!;
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = default!;
    public string? MatchedText { get; set; }
}

public sealed class AssetCandidateDto
{
    public Guid AssetId { get; set; }
    public string Ticker { get; set; } = default!;
    public string Name { get; set; } = default!;
}
