using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.ValueObject
{
    public sealed record Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        // Parameterless constructor for EF
        private Money()
        {
        }

        // Public constructor with property-matching parameters for EF
        public Money(decimal amount, string currency)
        {
            Currency = NormalizeCurrency(currency);
            Amount = amount;
        }

        public static Money Of(decimal amount, string currency)
            => new(amount, currency);

        public static Money Zero(string currency) => new(0m, currency);

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal factor)
            => new Money(Amount * factor, Currency);

        public bool IsNegative() => Amount < 0m;

        private void EnsureSameCurrency(Money other)
        {
            if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}");
        }

        private static string NormalizeCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.", nameof(currency));

            return currency.Trim().ToUpperInvariant();
        }
    }
}
