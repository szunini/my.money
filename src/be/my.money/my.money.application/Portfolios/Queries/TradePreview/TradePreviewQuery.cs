namespace my.money.application.Portfolios.Queries.TradePreview;

public sealed record TradePreviewQuery(
    Guid AssetId,
    decimal Quantity,
    string Side
);
