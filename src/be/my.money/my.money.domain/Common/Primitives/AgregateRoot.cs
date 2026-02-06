using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.Primitives
{
    public abstract class AggregateRoot<TId> : Entity<TId>
    {
        private readonly List<DomainEvent> _domainEvents = new();

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void Raise(DomainEvent @event) => _domainEvents.Add(@event);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
