using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.Infraestructure.Persistence.Configurations
{
    internal class TradeConfig : IEntityTypeConfiguration<Trade>
    {
        public void Configure(EntityTypeBuilder<Trade> builder)
        {
            // Primary key
            builder.HasKey(t => t.Id);

            // Quantity as value object
            builder.OwnsOne(t => t.Quantity, qtyBuilder =>
            {
                qtyBuilder.Property(q => q.Value)
                    .HasColumnName("Quantity")
                    .HasPrecision(18, 0)
                    .IsRequired();
            });

            // Price as Money value object
            builder.OwnsOne(t => t.Price, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("PriceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // TotalAmount as Money value object
            builder.OwnsOne(t => t.TotalAmount, totalBuilder =>
            {
                totalBuilder.Property(m => m.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                totalBuilder.Property(m => m.Currency)
                    .HasColumnName("TotalCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
