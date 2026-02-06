using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Common.ValueObject;

namespace my.money.Infraestructure.Persistence.Seeding;

internal sealed class DbSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ApplicationDbContext db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        try
        {
            // 1. Ensure database is created (dev only - in prod use migrations explicitly)
            if (_db.Database.IsRelational())
            {
                await _db.Database.MigrateAsync(ct);
                _logger.LogInformation("Database migrations applied successfully.");
            }

            // 2. Seed Assets
            await SeedAssetsAsync(ct);

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedAssetsAsync(CancellationToken ct)
    {
        // Load existing tickers (normalized) for duplicate check
        var existingTickers = await _db.Assets
            .Select(a => a.Ticker.Value)
            .ToListAsync(ct);

        var existingTickersSet = new HashSet<string>(
            existingTickers,
            StringComparer.OrdinalIgnoreCase
        );

        var seedData = AssetSeedData.GetAssets();
        var assetsToAdd = new List<Asset>();

        foreach (var seed in seedData)
        {
            // Skip if already exists
            if (existingTickersSet.Contains(seed.Ticker))
            {
                _logger.LogDebug("Asset {Ticker} already exists, skipping.", seed.Ticker);
                continue;
            }

            // Create Asset using domain constructor + value object
            var ticker = Ticker.Of(seed.Ticker);
            var asset = new Asset(ticker, seed.Name, seed.Type, seed.Currency);
            assetsToAdd.Add(asset);

            _logger.LogInformation(
                "Adding asset: {Ticker} - {Name} ({Type})",
                seed.Ticker,
                seed.Name,
                seed.Type
            );
        }

        if (assetsToAdd.Count > 0)
        {
            await _db.Assets.AddRangeAsync(assetsToAdd, ct);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Seeded {Count} new assets.", assetsToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new assets to seed.");
        }
    }
}
