using my.money.domain.Common.Primitives;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Aggregates.Portfolios
{
    public sealed class Trade : Entity<Guid>
    {
        public Guid PortfolioId { get; private set; }
        public Guid AssetId { get; private set; }

        public TradeSide Side { get; private set; }

        public Quantity Quantity { get; private set; } = default!;
        public Money Price { get; private set; } = default!;      // precio unitario
        public Money TotalAmount { get; private set; } = default!; // qty * price

        public DateTime ExecutedAtUtc { get; private set; }

        private Trade() { } // EF

        internal Trade(Guid portfolioId, Guid assetId, TradeSide side, Quantity quantity, Money price, DateTime executedAtUtc)
        {
            if (quantity.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (price.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(price));

            Id = Guid.NewGuid();
            PortfolioId = portfolioId;
            AssetId = assetId;
            Side = side;
            Quantity = quantity;
            Price = price;
            TotalAmount = price.Multiply(quantity.Value);
            ExecutedAtUtc = DateTime.SpecifyKind(executedAtUtc, DateTimeKind.Utc);
        }
    }
}
