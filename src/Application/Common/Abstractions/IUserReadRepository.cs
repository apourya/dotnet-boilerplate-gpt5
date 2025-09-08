using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBoilerplate.Application.Users;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IUserReadRepository
    {
        Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<UserDto>> ListAsync(DateTime? afterCreatedUtc, Guid? afterId, int pageSize, CancellationToken ct = default);
    }
}
