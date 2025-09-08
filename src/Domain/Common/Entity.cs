using System;
using System.Collections.Generic;

namespace EnterpriseBoilerplate.Domain.Common
{
    public abstract class Entity<TId>
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public TId Id { get; protected set; } = default!;
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(IDomainEvent evt) => _domainEvents.Add(evt);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
