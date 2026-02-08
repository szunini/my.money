namespace my.money.application.Ports.Persistence;

public interface IQuoteRepository
{
    /// <summary>
    /// Gets the latest quote for each asset at or before the specified date.
    /// Returns a dictionary keyed by AssetId with the corresponding quote.
    /// </summary>
    Task<Dictionary<Guid, my.money.domain.Aggregates.Assets.Quote>> GetLatestQuotesAtOrBeforeAsync(
        List<Guid> assetIds,
        DateTime asOfUtc,
        CancellationToken ct = default);
}
