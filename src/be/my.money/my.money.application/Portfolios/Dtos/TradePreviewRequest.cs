namespace my.money.application.Portfolios.Dtos;

public sealed record TradePreviewRequest(
    Guid AssetId,
    decimal Quantity,
    string Side
);
