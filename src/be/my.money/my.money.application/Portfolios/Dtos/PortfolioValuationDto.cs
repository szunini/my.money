namespace my.money.application.Portfolios.Dtos;

public sealed record PortfolioValuationDto(
    DateTime AsOfUtc,
    decimal CashBalanceAmount,
    string Currency,
    List<PortfolioValuationLineDto> Holdings,
    decimal TotalHoldingsValue,
    decimal TotalPortfolioValue
);
