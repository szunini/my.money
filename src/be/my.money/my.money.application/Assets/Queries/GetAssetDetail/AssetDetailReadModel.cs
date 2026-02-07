namespace my.money.application.Assets.Queries.GetAssetDetail;

public sealed record AssetDetailReadModel(
    Guid AssetId,
    string Symbol,
    string Name,
    string Type,
    decimal CurrentPrice,
    decimal QuantityOwned
);
