using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using StackExchange.Redis;

namespace EnterpriseBoilerplate.Infrastructure.Caching
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        public RedisCacheService(IConnectionMultiplexer mux)
        {
            _db = mux.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var val = await _db.StringGetAsync(key);
            if (val.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(val!);
        }

        public Task RemoveAsync(string key, CancellationToken ct = default) => _db.KeyDeleteAsync(key);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(value);
            return _db.StringSetAsync(key, json, ttl);
        }
    }
}
