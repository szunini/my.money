using my.money.domain.Common.Primitives;

namespace my.money.domain.Aggregates.News;

public sealed class NewsItem : AggregateRoot<Guid>
{
    public string Source { get; private set; } = default!;  // "Cronista" | "Infobae"
    public string Title { get; private set; } = default!;
    public string Url { get; private set; } = default!;     // Unique
    public DateTime? PublishedAtUtc { get; private set; }
    public string? Summary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<NewsMention> _mentions = new();
    public IReadOnlyCollection<NewsMention> Mentions => _mentions.AsReadOnly();

    private NewsItem() { } // EF

    public NewsItem(string source, string title, string url, DateTime? publishedAtUtc, string? summary = null)
    {
        Id = Guid.NewGuid();
        Source = source;
        Title = title;
        Url = url;
        PublishedAtUtc = publishedAtUtc;
        Summary = summary;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AddMention(NewsMention mention)
    {
        if (mention == null)
            throw new ArgumentNullException(nameof(mention));

        _mentions.Add(mention);
    }
}
