using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Common.Behaviors;

namespace EnterpriseBoilerplate.Application.Users.Commands
{
    public sealed record UpdateUserCommand(System.Guid Id, string Username, string Email)
        : IRequest<UserDto>, ITransactionalRequest;

    public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        }
    }

    public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly IUserRepository _repo;
        private readonly ICacheService _cache;

        public UpdateUserHandler(IUserRepository repo, ICacheService cache)
        {
            _repo = repo; _cache = cache;
        }

        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
        {
            var u = await _repo.GetByIdAsync(request.Id, ct) ?? throw new System.InvalidOperationException("User not found");
            // naive uniqueness validation (should be optimized)
            var byUsername = await _repo.GetByUsernameAsync(request.Username, ct);
            if (byUsername is not null && byUsername.Id != u.Id) throw new System.InvalidOperationException("Username already exists");
            var byEmail = await _repo.GetByEmailAsync(request.Email, ct);
            if (byEmail is not null && byEmail.Id != u.Id) throw new System.InvalidOperationException("Email already exists");

            u.Update(request.Username, request.Email);
            _repo.Update(u);

            await _cache.RemoveAsync($"users:{u.Id}", ct);

            return new UserDto(u.Id, u.Username, u.Email, u.Roles, u.CreatedUtc, u.UpdatedUtc);
        }
    }
}
