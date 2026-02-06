using my.money.domain.Aggregates.Assets;

namespace my.money.application.Ports.Queries;

public interface IAssetQueryService
{
    Task<IEnumerable<AssetDto>> GetAllAssetsAsync(CancellationToken ct = default);
    Task<AssetDto?> GetAssetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AssetDto?> GetAssetByTickerAsync(string ticker, CancellationToken ct = default);
}

public record AssetDto(
    Guid Id,
    string Ticker,
    string Name,
    string Type,
    string Currency
);
