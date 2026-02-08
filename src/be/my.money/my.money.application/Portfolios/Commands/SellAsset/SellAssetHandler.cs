using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.application.Portfolios.Commands.SellAsset;

public sealed class SellAssetHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public SellAssetHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TradeResponse> HandleAsync(SellAssetCommand command, CancellationToken ct = default)
    {
        // 1. Validate authentication
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedAccessException("User is not authenticated");

        // 2. Validate quantity
        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(command.Quantity));

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // 3. Get portfolio (must exist for sell)
            var portfolio = await _unitOfWork.Portfolios.GetByUserIdWithHoldingsAsync(_currentUser.UserId, ct);
            if (portfolio is null)
                throw new InvalidOperationException("Portfolio not found. You need to buy assets first.");

            // 4. Get asset and validate it exists
            var asset = await _unitOfWork.Assets.GetByIdAsync(command.AssetId, ct);
            if (asset is null)
                throw new InvalidOperationException($"Asset with ID {command.AssetId} not found");

            // 5. Get latest price
            var latestQuote = asset.GetLatestQuote();
            if (latestQuote is null)
                throw new InvalidOperationException($"No price available for asset {asset.Ticker.Value}");

            var unitPrice = latestQuote.Price;

            // 6. Execute sell (domain logic validates holdings)
            var trade = portfolio.Sell(
                command.AssetId,
                Quantity.Of(command.Quantity),
                unitPrice,
                DateTime.UtcNow
            );

            // 7. Save changes
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync();

            return new TradeResponse(
                trade.Id,
                command.AssetId,
                asset.Ticker.Value,
                "Sell",
                command.Quantity,
                unitPrice.Amount,
                trade.TotalAmount.Amount,
                unitPrice.Currency,
                trade.ExecutedAtUtc,
                portfolio.CashBalance.Amount,
                $"Successfully sold {command.Quantity} units of {asset.Ticker.Value}"
            );
        }
        catch (InvalidOperationException ex) when (ex.Message == PortfolioErrors.InsufficientQuantity)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new InvalidOperationException("Insufficient quantity to sell", ex);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
