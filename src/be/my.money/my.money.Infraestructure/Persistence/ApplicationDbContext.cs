using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Aggregates.Portfolios;
using my.money.domain.Aggregates.News;
using my.money.domain.Common.Primitives;
using my.money.Infraestructure.Authentication;

namespace my.money.Infraestructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domain aggregates
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    
    // Optional: explicit access to child entities for queries
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<NewsMention> NewsMentions => Set<NewsMention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Domain events are not mapped as entities
        modelBuilder.Ignore<DomainEvent>();

        // Configuración de entidades
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
