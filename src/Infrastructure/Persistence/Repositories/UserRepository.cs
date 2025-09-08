using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Domain.Users;

namespace EnterpriseBoilerplate.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(WriteDbContext db) : base(db) { }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            _db.Users.Include("_roles").SingleOrDefaultAsync(u => u.Username == username, ct);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            _db.Users.Include("_roles").SingleOrDefaultAsync(u => u.Email == email, ct);
    }
}
