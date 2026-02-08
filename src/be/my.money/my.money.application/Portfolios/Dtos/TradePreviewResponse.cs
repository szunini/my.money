namespace my.money.application.Portfolios.Dtos;

public sealed record TradePreviewResponse(
    Guid AssetId,
    string AssetTicker,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal TotalAmount,
    string Currency,
    decimal AvailableCash,
    decimal AvailableQuantity,
    bool IsAllowed,
    string Message
);
