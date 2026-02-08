using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.Infraestructure.Persistence.Configurations
{
    internal class HoldingConfig : IEntityTypeConfiguration<Holding>
    {
        public void Configure(EntityTypeBuilder<Holding> builder)
        {
            // Primary key - GUID generated client-side
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id).ValueGeneratedNever();

            builder.Property(h => h.PortfolioId)
                .IsRequired();

            builder.Property(h => h.AssetId)
                .IsRequired();

            // Value object for quantity
            builder.OwnsOne(h => h.Quantity, qtyBuilder =>
            {
                qtyBuilder.Property(q => q.Value)
                    .HasColumnName("Quantity")
                    .HasPrecision(18, 0)
                    .IsRequired();
            });

            // Value object for average cost
            builder.OwnsOne(h => h.AverageCost, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("AverageCostAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("AverageCostCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
