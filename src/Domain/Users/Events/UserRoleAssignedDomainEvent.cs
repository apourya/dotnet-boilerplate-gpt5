using System;
using EnterpriseBoilerplate.Domain.Common;

namespace EnterpriseBoilerplate.Domain.Users.Events
{
    public sealed class UserRoleAssignedDomainEvent : IDomainEvent
    {
        public Guid UserId { get; }
        public string Role { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public UserRoleAssignedDomainEvent(Guid userId, string role) { UserId = userId; Role = role; }
    }
}
