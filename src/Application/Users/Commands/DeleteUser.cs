using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Common.Behaviors;

namespace EnterpriseBoilerplate.Application.Users.Commands
{
    public sealed record DeleteUserCommand(System.Guid Id) : IRequest, ITransactionalRequest;

    public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IUserRepository _repo;
        private readonly ICacheService _cache;
        public DeleteUserHandler(IUserRepository repo, ICacheService cache)
        {
            _repo = repo; _cache = cache;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken ct)
        {
            var u = await _repo.GetByIdAsync(request.Id, ct) ?? throw new System.InvalidOperationException("User not found");
            _repo.Remove(u);
            await _cache.RemoveAsync($"users:{u.Id}", ct);
        }
    }
}
