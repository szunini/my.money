namespace my.money.application.Portfolios.Dtos;

public record DashboardDto(
    decimal CashBalanceAmount,
    IEnumerable<HoldingItemDto> Holdings,
    IEnumerable<AssetItemDto> AvailableAssets
);

public record HoldingItemDto(
    Guid AssetId,
    string Ticker,
    string Name,
    string Type,
    decimal Quantity
);

public record AssetItemDto(
    Guid AssetId,
    string Ticker,
    string Name,
    string Type
);
