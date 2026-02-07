namespace my.money.application.Portfolios.Dtos;

public record DashboardValuationDto(
    decimal CashBalance,
    decimal TotalHoldingsValue,
    decimal TotalPortfolioValue,
    IReadOnlyList<HoldingValuationItemDto> Holdings,
    IReadOnlyList<TradableAssetDto> TradableAssets
);

public record HoldingValuationItemDto(
    Guid AssetId,
    string Ticker,
    string Name,
    string Type,
    decimal Quantity,
    decimal? LatestPrice,
    DateTime? LatestPriceAsOfUtc,
    decimal? HoldingValue
);

public record TradableAssetDto(
    Guid AssetId,
    string Ticker,
    string Name,
    string Type,
    decimal? LatestPrice,
    DateTime? LatestPriceAsOfUtc
);
