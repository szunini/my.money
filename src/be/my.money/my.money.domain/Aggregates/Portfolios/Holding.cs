using my.money.domain.Common.Primitives;
using my.money.domain.Common.ValueObject;
using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Aggregates.Portfolios
{
    public sealed class Holding : Entity<Guid>
    {
        public Guid PortfolioId { get; private set; }
        public Guid AssetId { get; private set; }

        public Quantity Quantity { get; private set; } = Quantity.Zero();
        public Money AverageCost { get; private set; } = Money.Zero("ARS"); // currency del asset
        public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

        private Holding() { } // EF

        internal Holding(Guid portfolioId, Guid assetId, string currency)
        {
            Id = Guid.NewGuid();
            PortfolioId = portfolioId;
            AssetId = assetId;
            Quantity = Quantity.Zero();
            AverageCost = Money.Zero(currency);
        }

        internal void ApplyBuy(Quantity qty, Money unitPrice)
        {
            // promedio ponderado: (q0*avg + q*price) / (q0+q)
            if (qty.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(qty));

            if (!string.Equals(AverageCost.Currency, unitPrice.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(PortfolioErrors.CurrencyMismatch);

            var currentValue = AverageCost.Multiply(Quantity.Value);
            var buyValue = unitPrice.Multiply(qty.Value);

            var newQty = Quantity.Add(qty);
            var newAvg = (currentValue.Amount + buyValue.Amount) / newQty.Value;

            Quantity = newQty;
            AverageCost = Money.Of(newAvg, unitPrice.Currency);
            UpdatedAtUtc = DateTime.UtcNow;
        }

        internal void ApplySell(Quantity qty)
        {
            if (qty.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(qty));
            Quantity = Quantity.Subtract(qty);

            // si queda en 0, reseteo avg cost
            if (Quantity.IsZero())
                AverageCost = Money.Zero(AverageCost.Currency);

            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
