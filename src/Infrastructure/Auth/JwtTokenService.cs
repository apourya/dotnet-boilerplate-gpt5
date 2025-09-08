using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseBoilerplate.Infrastructure.Auth
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        public JwtTokenService(IConfiguration config) { _config = config; }

        public string GenerateToken(User user, IEnumerable<string> roles)
        {
            var issuer = _config["Auth:Jwt:Issuer"] ?? "EnterpriseBoilerplate";
            var audience = _config["Auth:Jwt:Audience"] ?? "EnterpriseBoilerplate.Clients";
            var key = _config["Auth:Jwt:Key"] ?? "insecure-dev-key-change-me";
            var expires = DateTime.UtcNow.AddHours(1);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
