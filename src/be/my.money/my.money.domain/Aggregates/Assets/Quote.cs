using my.money.domain.Common.Primitives;
using my.money.domain.Common.ValueObject;

namespace my.money.domain.Aggregates.Assets;

public sealed class Quote : Entity<Guid>
{
public Guid AssetId { get; private set; }
public Money Price { get; private set; } = default!;
public DateTime AsOfUtc { get; private set; }
public string Source { get; private set; } = "manual";

private Quote() { } // EF

internal Quote(Guid assetId, Money price, DateTime asOfUtc, string source)
{
    Id = Guid.NewGuid();
    AssetId = assetId;
    Price = price;
    AsOfUtc = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
    Source = string.IsNullOrWhiteSpace(source) ? "manual" : source.Trim();
}
}