using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IReadDbConnectionFactory
    {
        Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
    }
}
