using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
