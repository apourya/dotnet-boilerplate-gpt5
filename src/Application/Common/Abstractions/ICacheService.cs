using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
    }
}
