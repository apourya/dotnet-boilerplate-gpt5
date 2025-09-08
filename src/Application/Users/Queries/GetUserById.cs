using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Common.Behaviors;

namespace EnterpriseBoilerplate.Application.Users.Queries
{
    public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>, ICacheableQuery
    {
        public string CacheKey => $"users:{Id}";
        public TimeSpan? Ttl => TimeSpan.FromMinutes(5);
    }

    public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto>
    {
        private readonly IUserReadRepository _reads;
        public GetUserByIdHandler(IUserReadRepository reads) { _reads = reads; }

        public Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
            => _reads.GetByIdAsync(request.Id, ct);
    }
}
