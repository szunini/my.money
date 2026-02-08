using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.domain.Enum;

namespace my.money.application.Portfolios.Queries.TradePreview;

public sealed class TradePreviewHandler
{
    private const decimal DefaultInitialCash = 1_000_000m;

    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public TradePreviewHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TradePreviewResponse> HandleAsync(TradePreviewQuery query, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        if (query.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(query.Quantity));

        if (!Enum.TryParse<TradeSide>(query.Side, true, out var side))
            throw new ArgumentException("Side must be Buy or Sell", nameof(query.Side));

        var asset = await _unitOfWork.Assets.GetByIdAsync(query.AssetId, ct);
        if (asset is null)
            throw new InvalidOperationException($"Asset with ID {query.AssetId} not found");

        var latestQuote = asset.GetLatestQuote();
        if (latestQuote is null)
            throw new InvalidOperationException($"No price available for asset {asset.Ticker.Value}");

        var unitPrice = latestQuote.Price;
        var totalAmount = unitPrice.Amount * query.Quantity;

        var portfolio = await _unitOfWork.Portfolios.GetByUserIdWithHoldingsAsync(_currentUser.UserId, ct);
        var availableCash = portfolio?.CashBalance.Amount ?? DefaultInitialCash;

        decimal availableQuantity = 0m;
        if (portfolio is not null)
        {
            var holding = portfolio.Holdings.SingleOrDefault(h => h.AssetId == query.AssetId);
            if (holding is not null)
                availableQuantity = holding.Quantity.Value;
        }

        var isAllowed = side == TradeSide.Buy
            ? availableCash >= totalAmount
            : availableQuantity >= query.Quantity;

        var message = side == TradeSide.Buy
            ? isAllowed ? "Sufficient cash for buy" : "Insufficient cash for buy"
            : isAllowed ? "Sufficient quantity for sell" : "Insufficient quantity for sell";

        return new TradePreviewResponse(
            asset.Id,
            asset.Ticker.Value,
            side.ToString(),
            query.Quantity,
            unitPrice.Amount,
            totalAmount,
            unitPrice.Currency,
            availableCash,
            availableQuantity,
            isAllowed,
            message
        );
    }
}
