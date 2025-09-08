using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Common.Behaviors;
using EnterpriseBoilerplate.Domain.Users;

namespace EnterpriseBoilerplate.Application.Users.Commands
{
    public sealed record RegisterUserCommand(string Username, string Email, string Password)
        : IRequest<UserDto>, ITransactionalRequest;

    public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        }
    }

    public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, UserDto>
    {
        private readonly IUserRepository _repo;
        private readonly IPasswordHasher _hasher;

        public RegisterUserHandler(IUserRepository repo, IPasswordHasher hasher)
        {
            _repo = repo; _hasher = hasher;
        }

        public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken ct)
        {
            if (await _repo.GetByUsernameAsync(request.Username, ct) is not null)
                throw new System.InvalidOperationException("Username already exists");
            if (await _repo.GetByEmailAsync(request.Email, ct) is not null)
                throw new System.InvalidOperationException("Email already exists");

            var hash = _hasher.Hash(request.Password);
            var user = User.Register(request.Username, request.Email, hash);
            user.AssignRole("User"); // default role
            await _repo.AddAsync(user, ct);

            return new UserDto(user.Id, user.Username, user.Email, user.Roles, user.CreatedUtc, user.UpdatedUtc);
        }
    }
}
