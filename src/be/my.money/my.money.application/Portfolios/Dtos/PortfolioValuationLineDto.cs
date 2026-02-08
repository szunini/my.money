namespace my.money.application.Portfolios.Dtos;

public sealed record PortfolioValuationLineDto(
    Guid AssetId,
    string Ticker,
    string Name,
    decimal Quantity,
    decimal PriceAsOf,
    DateTime? PriceAsOfUtc,
    decimal Valuation
);
