using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace my.money.Infraestructure.Persistence.Seeding;

public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Seeds the database with initial data (Assets, etc.)
    /// Call this in Program.cs after app is built: await app.SeedDatabaseAsync();
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<DbSeeder>>();

            var seeder = new DbSeeder(db, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DatabaseSeeding");
            logger.LogError(ex, "An error occurred during database seeding.");
            
            // In development, you might want to rethrow to catch issues early
            if (app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }
}
