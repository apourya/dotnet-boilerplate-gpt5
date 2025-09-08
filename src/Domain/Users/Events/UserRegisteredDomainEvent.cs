using System;
using EnterpriseBoilerplate.Domain.Common;

namespace EnterpriseBoilerplate.Domain.Users.Events
{
    public sealed class UserRegisteredDomainEvent : IDomainEvent
    {
        public Guid UserId { get; }
        public string Username { get; }
        public string Email { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public UserRegisteredDomainEvent(Guid userId, string username, string email)
        {
            UserId = userId; Username = username; Email = email;
        }
    }
}
