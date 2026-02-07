namespace my.money.application.Assets.Dtos;

public record AssetDetailDto(
    Guid AssetId,
    string Symbol,
    string Name,
    string Type,
    decimal CurrentPrice,
    decimal QuantityOwned,
    decimal Valuation
);
