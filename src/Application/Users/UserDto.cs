using System;
using System.Collections.Generic;

namespace EnterpriseBoilerplate.Application.Users
{
    public sealed record UserDto(Guid Id, string Username, string Email, IEnumerable<string> Roles, DateTime CreatedUtc, DateTime UpdatedUtc);
}
