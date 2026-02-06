using my.money.domain.Common.Primitives;
using my.money.domain.Common.ValueObject;
using my.money.domain.Enum;

namespace my.money.domain.Aggregates.Assets
{
    public sealed class Asset : AggregateRoot<Guid>
    {
        public Ticker Ticker { get; private set; } = default!;
        public string Name { get; private set; } = default!;
        public AssetType Type { get; private set; }
        public string Currency { get; private set; } = "ARS";

        private readonly List<Quote> _quotes = new();
        public IReadOnlyCollection<Quote> Quotes => _quotes.AsReadOnly();

        private Asset() { } // EF

        public Asset(Ticker ticker, string name, AssetType type, string currency = "ARS")
        {
            Id = Guid.NewGuid();
            Ticker = ticker;
            Name = NormalizeName(name);
            Type = type;
            Currency = currency.Trim().ToUpperInvariant();
        }

        public Quote AddQuote(Money price, DateTime asOfUtc, string source = "manual")
        {
            if (!string.Equals(price.Currency, Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Quote currency {price.Currency} must match asset currency {Currency}.");

            var quote = new Quote(Id, price, asOfUtc, source);
            _quotes.Add(quote);
            return quote;
        }

        public Quote? GetLatestQuote()
            => _quotes
                .OrderByDescending(q => q.AsOfUtc)
                .FirstOrDefault();

        public Quote? GetLatestQuoteAtOrBefore(DateTime whenUtc)
            => _quotes
                .Where(q => q.AsOfUtc <= whenUtc)
                .OrderByDescending(q => q.AsOfUtc)
                .FirstOrDefault();

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
            return name.Trim();
        }
    }
}
