using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;

namespace EnterpriseBoilerplate.Application.Users.Queries
{
    public sealed record ListUsersQuery(DateTime? AfterCreatedUtc, Guid? AfterId, int PageSize = 50)
        : IRequest<IReadOnlyList<UserDto>>;

    public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
    {
        private readonly IUserReadRepository _reads;
        public ListUsersHandler(IUserReadRepository reads) { _reads = reads; }

        public Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery request, CancellationToken ct)
            => _reads.ListAsync(request.AfterCreatedUtc, request.AfterId, request.PageSize, ct);
    }
}
