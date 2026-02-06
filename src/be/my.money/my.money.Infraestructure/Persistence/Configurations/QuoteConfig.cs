using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.Assets;

namespace my.money.Infraestructure.Persistence.Configurations
{
    internal class QuoteConfig : IEntityTypeConfiguration<Quote>
    {
        public void Configure(EntityTypeBuilder<Quote> builder)
        {
            // Configure primary key
            builder.HasKey(q => q.Id);

            // Configure Money as an owned type for Price
            builder.OwnsOne(q => q.Price, priceBuilder =>
            {
                priceBuilder.Property(p => p.Amount)
                    .HasColumnName("PriceAmount")
                    .HasPrecision(18, 2)  // 18 total digits, 4 after decimal
                    .IsRequired();

                priceBuilder.Property(p => p.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
