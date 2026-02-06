using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.ValueObject
{
    public sealed record Quantity
    {
        public decimal Value { get; }

        // Parameterless constructor for EF
        private Quantity()
        {
        }

        // Public constructor with property-matching parameter for EF
        public Quantity(decimal value)
        {
            if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value), "Quantity cannot be negative.");
            Value = value;
        }

        public static Quantity Of(decimal value) => new(value);

        public static Quantity Zero() => new(0m);

        public Quantity Add(Quantity other) => new(Value + other.Value);

        public Quantity Subtract(Quantity other)
        {
            if (Value < other.Value) throw new InvalidOperationException("Insufficient quantity.");
            return new Quantity(Value - other.Value);
        }

        public bool IsZero() => Value == 0m;
    }
}
