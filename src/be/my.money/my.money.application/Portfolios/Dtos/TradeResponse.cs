namespace my.money.application.Portfolios.Dtos;

public record TradeResponse(
    Guid TradeId,
    Guid AssetId,
    string AssetTicker,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal TotalAmount,
    string Currency,
    DateTime ExecutedAtUtc,
    decimal RemainingCash,
    string Message
);
