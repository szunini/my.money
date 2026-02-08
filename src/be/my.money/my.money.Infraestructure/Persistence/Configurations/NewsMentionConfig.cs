using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.News;

namespace my.money.Infraestructure.Persistence.Configurations;

internal class NewsMentionConfig : IEntityTypeConfiguration<NewsMention>
{
    public void Configure(EntityTypeBuilder<NewsMention> builder)
    {
        // Primary key
        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.Confidence)
            .HasPrecision(3, 2);  // DECIMAL(3,2) for 0.00 to 1.00

        builder.Property(m => m.Explanation)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.MatchedText)
            .HasMaxLength(200);

        builder.Property(m => m.DetectedAtUtc)
            .IsRequired();

        // Foreign keys
        builder.HasOne<NewsItem>()
            .WithMany(n => n.Mentions)
            .HasForeignKey(m => m.NewsItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => new { m.AssetId, m.DetectedAtUtc })
            .IsDescending(false, true);
    }
}
