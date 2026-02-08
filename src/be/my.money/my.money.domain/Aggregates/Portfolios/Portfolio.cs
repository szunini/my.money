using my.money.domain.Common.Primitives;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;

namespace my.money.domain.Aggregates.Portfolios
{
    public sealed class Portfolio : AggregateRoot<Guid>
    {
        public string UserId { get; private set; } = default!;

        public Money CashBalance { get; private set; } = Money.Zero("ARS");
        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

        private readonly List<Holding> _holdings = new();
        public IReadOnlyCollection<Holding> Holdings => _holdings.AsReadOnly();

        private readonly List<Trade> _trades = new();
        public IReadOnlyCollection<Trade> Trades => _trades.AsReadOnly();

        private Portfolio() { } // EF

        public Portfolio(string userId, Money initialCash)
        {
            Id = Guid.NewGuid();
            UserId = string.IsNullOrWhiteSpace(userId) ? throw new ArgumentException("UserId is required.", nameof(userId)) : userId;
            CashBalance = initialCash;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void Deposit(Money amount)
        {
            if (amount.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));
            EnsureSameCurrency(amount);

            CashBalance = CashBalance.Add(amount);
        }

        public Trade Buy(Guid assetId, string assetCurrency, Quantity qty, Money unitPrice, DateTime? atUtc = null)
        {
            if (qty.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(qty));
            if (unitPrice.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice));

            // currency guardrails
            if (!string.Equals(assetCurrency, unitPrice.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(PortfolioErrors.CurrencyMismatch);

            EnsureSameCurrency(unitPrice);

            var cost = unitPrice.Multiply(qty.Value);
            if (CashBalance.Amount < cost.Amount)
                throw new InvalidOperationException(PortfolioErrors.InsufficientCash);

            // cash
            CashBalance = CashBalance.Subtract(cost);

            // holding
            var holding = GetOrCreateHolding(assetId, assetCurrency);
            holding.ApplyBuy(qty, unitPrice);

            // trade record - clone Money to avoid owned-type tracking issues
            var clonedPrice = Money.Of(unitPrice.Amount, unitPrice.Currency);
            var trade = new Trade(Id, assetId, TradeSide.Buy, qty, clonedPrice, atUtc ?? DateTime.UtcNow);
            _trades.Add(trade);

            return trade;
        }

        public Trade Sell(Guid assetId, Quantity qty, Money unitPrice, DateTime? atUtc = null)
        {
            if (qty.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(qty));
            if (unitPrice.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice));

            EnsureSameCurrency(unitPrice);

            var holding = _holdings.SingleOrDefault(h => h.AssetId == assetId);
            if (holding is null)
                throw new InvalidOperationException(PortfolioErrors.InsufficientQuantity);

            // valida cantidad
            if (holding.Quantity.Value < qty.Value)
                throw new InvalidOperationException(PortfolioErrors.InsufficientQuantity);

            // holding
            holding.ApplySell(qty);

            // cash (ingreso)
            var proceeds = unitPrice.Multiply(qty.Value);
            CashBalance = CashBalance.Add(proceeds);

            // trade record - clone Money to avoid owned-type tracking issues
            var clonedPrice = Money.Of(unitPrice.Amount, unitPrice.Currency);
            var trade = new Trade(Id, assetId, TradeSide.Sell, qty, clonedPrice, atUtc ?? DateTime.UtcNow);
            _trades.Add(trade);

            return trade;
        }

        public bool HasHoldingFor(Guid assetId)
        {
            return _holdings.Any(h => h.AssetId == assetId);
        }

        public void EnsureHolding(Guid assetId, string currency)
        {
            GetOrCreateHolding(assetId, currency);
        }

        private Holding GetOrCreateHolding(Guid assetId, string currency)
        {
            var holding = _holdings.SingleOrDefault(h => h.AssetId == assetId);
            if (holding is not null) return holding;

            holding = new Holding(Id, assetId, currency);
            _holdings.Add(holding);
            return holding;
        }

        private void EnsureSameCurrency(Money other)
        {
            if (!string.Equals(CashBalance.Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(PortfolioErrors.CurrencyMismatch);
        }
    }
}
