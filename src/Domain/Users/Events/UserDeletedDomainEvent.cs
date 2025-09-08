using System;
using EnterpriseBoilerplate.Domain.Common;

namespace EnterpriseBoilerplate.Domain.Users.Events
{
    public sealed class UserDeletedDomainEvent : IDomainEvent
    {
        public Guid UserId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public UserDeletedDomainEvent(Guid userId) { UserId = userId; }
    }
}
