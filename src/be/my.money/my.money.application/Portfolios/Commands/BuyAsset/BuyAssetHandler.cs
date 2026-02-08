using my.money.application.Portfolios.Dtos;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.application.Ports.Queries;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.application.Portfolios.Commands.BuyAsset;

public sealed class BuyAssetHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public BuyAssetHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TradeResponse> HandleAsync(BuyAssetCommand command, CancellationToken ct = default)
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

            // 3. Get or create portfolio
            var portfolio = await _unitOfWork.Portfolios.GetByUserIdWithHoldingsAsync(_currentUser.UserId, ct);
            if (portfolio is null)
            {
                // Create portfolio with initial cash
                portfolio = new Portfolio(_currentUser.UserId, Money.Of(1_000_000m, "ARS"));
                await _unitOfWork.Portfolios.AddAsync(portfolio, ct);
            }

            // 4. Get asset and validate it exists
            var asset = await _unitOfWork.Assets.GetByIdAsync(command.AssetId, ct);
            if (asset is null)
                throw new InvalidOperationException($"Asset with ID {command.AssetId} not found");

            // 5. Get latest price
            var latestQuote = asset.GetLatestQuote();
            if (latestQuote is null)
                throw new InvalidOperationException($"No price available for asset {asset.Ticker.Value}");

            var unitPrice = latestQuote.Price;

            // 6. Ensure holding exists for this asset (kept tracked)
            if (!portfolio.HasHoldingFor(command.AssetId))
            {
                portfolio.EnsureHolding(command.AssetId, asset.Currency);
            }

            // 7. Execute buy (domain logic validates cash balance)
            var trade = portfolio.Buy(
                command.AssetId,
                asset.Currency,
                Quantity.Of(command.Quantity),
                unitPrice,
                DateTime.UtcNow
            );

            // 8. Save final changes
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync();

            return new TradeResponse(
                trade.Id,
                command.AssetId,
                asset.Ticker.Value,
                "Buy",
                command.Quantity,
                unitPrice.Amount,
                trade.TotalAmount.Amount,
                unitPrice.Currency,
                trade.ExecutedAtUtc,
                portfolio.CashBalance.Amount,
                $"Successfully bought {command.Quantity} units of {asset.Ticker.Value}"
            );
        }
        catch (InvalidOperationException ex) when (ex.Message == PortfolioErrors.InsufficientCash)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw new InvalidOperationException("Insufficient cash balance to complete this purchase", ex);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
