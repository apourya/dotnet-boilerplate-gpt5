using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseBoilerplate.Domain.Common;
using EnterpriseBoilerplate.Domain.Users.Events;

namespace EnterpriseBoilerplate.Domain.Users
{
    public sealed class User : AggregateRoot<Guid>
    {
        private readonly HashSet<UserRole> _roles = new();

        public string Username { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public DateTime CreatedUtc { get; private set; }
        public DateTime UpdatedUtc { get; private set; }
        public IReadOnlyCollection<string> Roles => _roles.Select(r => r.Name).ToArray();

        private User() { }

        private User(Guid id, string username, string email, string passwordHash)
        {
            Id = id;
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = CreatedUtc;
        }

        public static User Register(string username, string email, string passwordHash)
        {
            var u = new User(Guid.NewGuid(), username, email, passwordHash);
            u.AddDomainEvent(new UserRegisteredDomainEvent(u.Id, u.Username, u.Email));
            return u;
        }

        public void Update(string username, string email)
        {
            Username = username;
            Email = email;
            UpdatedUtc = DateTime.UtcNow;
            AddDomainEvent(new UserUpdatedDomainEvent(Id));
        }

        public void AssignRole(string role)
        {
            if (_roles.Add(new UserRole(role)))
            {
                UpdatedUtc = DateTime.UtcNow;
                AddDomainEvent(new UserRoleAssignedDomainEvent(Id, role));
            }
        }
    }
}
