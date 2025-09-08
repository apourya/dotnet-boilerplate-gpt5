using System.Collections.Generic;
using EnterpriseBoilerplate.Domain.Users;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, IEnumerable<string> roles);
    }
}
