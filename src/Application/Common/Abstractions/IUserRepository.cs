using System.Threading;
using System.Threading.Tasks;
using EnterpriseBoilerplate.Domain.Users;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    }
}
