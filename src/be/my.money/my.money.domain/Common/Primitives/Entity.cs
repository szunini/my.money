using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.Primitives
{
    public abstract class Entity<TId>
    {
        public TId Id { get; protected set; } = default!;

        // Para igualdad por identidad
        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TId> other) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            return EqualityComparer<TId>.Default.Equals(Id, other.Id);
        }

        public override int GetHashCode()
            => HashCode.Combine(GetType(), Id);
    }
}
