using System;
using EnterpriseBoilerplate.Domain.Common;

namespace EnterpriseBoilerplate.Domain.Users.Events
{
    public sealed class UserUpdatedDomainEvent : IDomainEvent
    {
        public Guid UserId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public UserUpdatedDomainEvent(Guid userId) { UserId = userId; }
    }
}
