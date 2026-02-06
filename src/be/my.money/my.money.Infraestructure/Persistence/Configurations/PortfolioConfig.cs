using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Common.ValueObject;

namespace my.money.Infraestructure.Persistence.Configurations
{
    internal class PortfolioConfig : IEntityTypeConfiguration<Portfolio>
    {
        public void Configure(EntityTypeBuilder<Portfolio> builder)
        {
            // Primary key
            builder.HasKey(p => p.Id);

            // CashBalance as Money value object
            builder.OwnsOne(p => p.CashBalance, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("CashBalanceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("CashBalanceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
