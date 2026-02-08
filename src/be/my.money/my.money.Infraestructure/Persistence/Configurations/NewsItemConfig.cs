using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using my.money.domain.Aggregates.News;

namespace my.money.Infraestructure.Persistence.Configurations;

internal class NewsItemConfig : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> builder)
    {
        // Primary key
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.PublishedAtUtc);

        builder.Property(n => n.Summary)
            .HasMaxLength(2000);

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired();

        // Indexes
        builder.HasIndex(n => n.Url)
            .IsUnique();

        builder.HasIndex(n => n.PublishedAtUtc)
            .IsDescending();

        // Configure relationship to NewsMention
        builder.HasMany(n => n.Mentions)
            .WithOne()
            .HasForeignKey(m => m.NewsItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map mentions collection
        builder.Navigation(n => n.Mentions)
            .AutoInclude();
    }
}
