using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using my.money.domain.Aggregates.Assets;
using my.money.domain.Common.ValueObject;
using my.money.Infraestructure.Persistence;
using Xunit;

namespace my.money.IntegrationTests.Persistence
{
    public class AssetQuoteInsertTests
    {
        [Fact]
        public async Task AddQuote_Should_Insert_New_Quote()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);
            var asset = new Asset(Ticker.Of("TEST"), "Test Asset", domain.Enum.AssetType.Stock, "USD");
            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            // Reload tracked asset
            var loaded = await context.Assets.Include(a => a.Quotes).FirstAsync(a => a.Id == asset.Id);
            loaded.AddQuote(Money.Of(123.45m, "USD"), DateTime.UtcNow, "test");
            await context.SaveChangesAsync();

            // Assert: Quotes table has new row
            var quotes = await context.Quotes.Where(q => q.AssetId == asset.Id).ToListAsync();
            Assert.Single(quotes);
            Assert.Equal(123.45m, quotes[0].Price.Amount);
            Assert.Equal("USD", quotes[0].Price.Currency);
        }
    }
}
