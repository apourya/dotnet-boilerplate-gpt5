using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnterpriseBoilerplate.Infrastructure.ReadModel
{
    public sealed class NpgsqlReadDbConnectionFactory : IReadDbConnectionFactory
    {
        private readonly string _connStr;
        public NpgsqlReadDbConnectionFactory(IConfiguration config)
        {
            _connStr = config.GetConnectionString("ReadDb") ?? config.GetConnectionString("WriteDb")!;
        }

        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
