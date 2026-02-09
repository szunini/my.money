using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;
using my.money.Infraestructure.Authentication;

namespace my.money.Infraestructure.Persistence.Seeding;

internal sealed class DbSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<DbSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
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

            // 2. Seed Test User
            await SeedTestUserAsync();

            // 2b. Seed Portfolio for test user
            await SeedTestPortfolioAsync(ct);

            // 3. Seed Assets
            await SeedAssetsAsync(ct);

            // 4. Seed Quotes for Assets
            await SeedQuotesAsync(ct);

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedTestUserAsync()
    {
        const string testEmail = "test@mymoney.com";
        const string testPassword = "Test123!";

        var existingUser = await _userManager.FindByEmailAsync(testEmail);
        if (existingUser is not null)
        {
            _logger.LogDebug("Test user {Email} already exists, skipping.", testEmail);
            return;
        }

        var testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(testUser, testPassword);
        if (result.Succeeded)
        {
            _logger.LogInformation("Test user created: {Email}", testEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create test user: {Errors}", errors);
            throw new InvalidOperationException($"Failed to create test user: {errors}");
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

    private async Task SeedQuotesAsync(CancellationToken ct)
    {
        // Get all assets with their quotes (tracking enabled to detect new quotes)
        var assets = await _db.Assets
            .Include(a => a.Quotes)
            .ToListAsync(ct);

        var random = new Random(42); // Seed for reproducibility
        var quotesToAdd = new List<Quote>();
        var now = DateTime.UtcNow;

        foreach (var asset in assets)
        {
            if (asset.Quotes.Any())
            {
                _logger.LogDebug("Asset {Ticker} already has quotes, skipping.", asset.Ticker.Value);
                continue;
            }

            // Generate realistic price based on asset type
            decimal price = asset.Type switch
            {
                AssetType.Stock => random.Next(900, 1501),      // 900-1500 ARS
                AssetType.Bond => random.Next(95, 111),         // 95-110 ARS
                _ => random.Next(100, 1001)                     // Default fallback
            };

            // Use domain method to add quote
            var quote = asset.AddQuote(
                Money.Of(price, asset.Currency),
                now,
                "seed"
            );

            quotesToAdd.Add(quote);
            _logger.LogInformation(
                "Added quote for {Ticker}: {Price} {Currency}",
                asset.Ticker.Value,
                price,
                asset.Currency
            );
        }

        if (quotesToAdd.Count > 0)
        {
            // Explicitly add quotes to the context so EF Core tracks them as Added, not Modified
            await _db.Quotes.AddRangeAsync(quotesToAdd, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} quotes for assets.", quotesToAdd.Count);
        }
        else
        {
            _logger.LogInformation("No new quotes to seed.");
        }
    }

    private async Task SeedTestPortfolioAsync(CancellationToken ct)
    {
        const string testEmail = "test@mymoney.com";
        const decimal initialCash = 1_000_000_000m;
        var testUser = await _userManager.FindByEmailAsync(testEmail);
        if (testUser is null)
        {
            _logger.LogWarning("Test user {Email} not found, skipping portfolio seed.", testEmail);
            return;
        }

        // Check if portfolio already exists for this user
        bool exists = await _db.Portfolios.AnyAsync(p => p.UserId == testUser.Id.ToString(), ct);
        if (exists)
        {
            _logger.LogDebug("Portfolio for test user {Email} already exists, skipping.", testEmail);
            return;
        }

        var portfolio = new my.money.domain.Aggregates.Portfolios.Portfolio(
            testUser.Id.ToString(),
            my.money.domain.Common.ValueObject.Money.Of(initialCash, "ARS")
        );
        await _db.Portfolios.AddAsync(portfolio, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded portfolio for test user {Email} with {Cash} ARS.", testEmail, initialCash);
    }
}
