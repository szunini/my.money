using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Common.ValueObject;

namespace my.money.Infraestructure.Persistence.Configurations
{
    internal class AssetConfig : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            // Primary key
            builder.HasKey(a => a.Id);

            // Ticker as value object
            builder.OwnsOne(a => a.Ticker, tickerBuilder =>
            {
                tickerBuilder.Property(t => t.Value)
                    .HasColumnName("Ticker")
                    .HasMaxLength(12)
                    .IsRequired();
            });

            // Configure relationship to Quotes
            builder.HasMany(a => a.Quotes)
                .WithOne()
                .HasForeignKey(q => q.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
