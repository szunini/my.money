using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.ValueObject
{
    public sealed record Ticker
    {
        public string Value { get; }

        // Parameterless constructor for EF
        private Ticker()
        {
        }

        // Public constructor with property-matching parameter for EF
        public Ticker(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Ticker is required.", nameof(value));

            var normalized = value.Trim().ToUpperInvariant();

            // Regla simple (ajustala a tu gusto)
            if (normalized.Length is < 2 or > 12)
                throw new ArgumentOutOfRangeException(nameof(value), "Ticker length must be 2..12.");

            Value = normalized;
        }

        public static Ticker Of(string value) => new(value);

        public override string ToString() => Value;
    }
}
