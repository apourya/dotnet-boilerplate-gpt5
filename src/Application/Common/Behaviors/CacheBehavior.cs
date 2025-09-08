using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;

namespace EnterpriseBoilerplate.Application.Common.Behaviors
{
    public interface ICacheableQuery
    {
        string CacheKey { get; }
        TimeSpan? Ttl { get; }
    }

    public sealed class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICacheService _cache;

        public CacheBehavior(ICacheService cache) { _cache = cache; }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (request is ICacheableQuery cq && cq.Ttl.HasValue)
            {
                var cached = await _cache.GetAsync<TResponse>(cq.CacheKey, ct);
                if (cached is not null) return cached;
                var response = await next();
                await _cache.SetAsync(cq.CacheKey, response, cq.Ttl.Value, ct);
                return response;
            }
            return await next();
        }
    }
}
