using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Common.Behaviors;

namespace EnterpriseBoilerplate.Application.Users.Commands
{
    public sealed record AssignRoleCommand(System.Guid Id, string Role) : IRequest<UserDto>, ITransactionalRequest;

    public sealed class AssignRoleValidator : AbstractValidator<AssignRoleCommand>
    {
        public AssignRoleValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
        }
    }

    public sealed class AssignRoleHandler : IRequestHandler<AssignRoleCommand, UserDto>
    {
        private readonly IUserRepository _repo;
        private readonly ICacheService _cache;
        public AssignRoleHandler(IUserRepository repo, ICacheService cache)
        {
            _repo = repo; _cache = cache;
        }

        public async Task<UserDto> Handle(AssignRoleCommand request, CancellationToken ct)
        {
            var u = await _repo.GetByIdAsync(request.Id, ct) ?? throw new System.InvalidOperationException("User not found");
            u.AssignRole(request.Role);
            _repo.Update(u);
            await _cache.RemoveAsync($"users:{u.Id}", ct);
            return new UserDto(u.Id, u.Username, u.Email, u.Roles, u.CreatedUtc, u.UpdatedUtc);
        }
    }
}
