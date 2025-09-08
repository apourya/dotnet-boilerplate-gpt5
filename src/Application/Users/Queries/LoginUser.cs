using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using EnterpriseBoilerplate.Application.Common.Abstractions;

namespace EnterpriseBoilerplate.Application.Users.Queries
{
    public sealed record LoginUserQuery(string UsernameOrEmail, string Password) : IRequest<AuthResultDto>;

    public sealed class LoginUserValidator : AbstractValidator<LoginUserQuery>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.UsernameOrEmail).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class LoginUserHandler : IRequestHandler<LoginUserQuery, AuthResultDto>
    {
        private readonly IUserRepository _repo;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public LoginUserHandler(IUserRepository repo, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _repo = repo; _hasher = hasher; _jwt = jwt;
        }

        public async Task<AuthResultDto> Handle(LoginUserQuery request, CancellationToken ct)
        {
            var user = await _repo.GetByUsernameAsync(request.UsernameOrEmail, ct)
                       ?? await _repo.GetByEmailAsync(request.UsernameOrEmail, ct);

            if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
                throw new System.UnauthorizedAccessException("Invalid credentials");

            var token = _jwt.GenerateToken(user, user.Roles);
            return new AuthResultDto(token, "Bearer", 3600);
        }
    }
}
