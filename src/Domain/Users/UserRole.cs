using System.Collections.Generic;
using EnterpriseBoilerplate.Domain.Common;

namespace EnterpriseBoilerplate.Domain.Users
{
    public sealed class UserRole : ValueObject
    {
        public string Name { get; private set; } = default!;
        private UserRole() { }
        public UserRole(string name) { Name = name; }
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name?.ToLowerInvariant();
        }
    }
}
