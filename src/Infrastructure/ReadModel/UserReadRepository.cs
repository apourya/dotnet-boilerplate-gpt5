using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Users;

namespace EnterpriseBoilerplate.Infrastructure.ReadModel
{
    public sealed class UserReadRepository : IUserReadRepository
    {
        private readonly IReadDbConnectionFactory _factory;
        public UserReadRepository(IReadDbConnectionFactory factory) { _factory = factory; }

        public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using var conn = await _factory.CreateOpenConnectionAsync(ct);
            var sql = @"SELECT u.""Id"" as Id, u.""Username"" as Username, u.""Email"" as Email, u.""CreatedUtc"" as CreatedUtc, u.""UpdatedUtc"" as UpdatedUtc,
                        array_agg(ur.""Role"" ORDER BY ur.""Role"") as Roles
                        FROM ""Users"" u
                        LEFT JOIN ""UserRoles"" ur ON ur.""UserId"" = u.""Id""
                        WHERE u.""Id"" = @Id
                        GROUP BY u.""Id"";";
            var r = (await conn.QueryAsync<Row>(sql, new { Id = id })).SingleOrDefault()
                    ?? throw new InvalidOperationException("User not found");
            return new UserDto(r.Id, r.Username, r.Email, r.Roles ?? Array.Empty<string>(), r.CreatedUtc, r.UpdatedUtc);
        }

        public async Task<IReadOnlyList<UserDto>> ListAsync(DateTime? afterCreatedUtc, Guid? afterId, int pageSize, CancellationToken ct = default)
        {
            using var conn = await _factory.CreateOpenConnectionAsync(ct);

            pageSize = Math.Min(200, Math.Max(1, pageSize));

            var sql = @"
        SELECT u.""Id"" as Id, u.""Username"" as Username, u.""Email"" as Email, u.""CreatedUtc"", u.""UpdatedUtc"",
               array_agg(ur.""Role"" ORDER BY ur.""Role"") as Roles
        FROM ""Users"" u
        LEFT JOIN ""UserRoles"" ur ON ur.""UserId"" = u.""Id""
        WHERE (
            @HasAfter = FALSE
            OR u.""CreatedUtc"" > @AfterCreatedUtc
            OR (u.""CreatedUtc"" = @AfterCreatedUtc AND u.""Id"" > @AfterId)
        )
        GROUP BY u.""Id""
        ORDER BY u.""CreatedUtc"", u.""Id""
        LIMIT @PageSize;
    ";

            // اگر یکی از پارامترهای after* نبود، HasAfter=false می‌شود و WHERE عملاً همه را برمی‌گرداند.
            var hasAfter = afterCreatedUtc.HasValue && afterId.HasValue;

            // مقادیر پیش‌فرض هم‌نوع برای جلوگیری از ابهام
            var afterCreatedValue = afterCreatedUtc ?? new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified); // مناسب برای timestamp (بدون TZ)
            var afterIdValue = afterId ?? Guid.Empty;

            var p = new Dapper.DynamicParameters();
            p.Add("HasAfter", hasAfter, System.Data.DbType.Boolean);
            p.Add("AfterCreatedUtc", afterCreatedValue, System.Data.DbType.DateTime); // ستون CreatedUtc به احتمال زیاد timestamp است
            p.Add("AfterId", afterIdValue, System.Data.DbType.Guid);
            p.Add("PageSize", pageSize, System.Data.DbType.Int32);

            var rows = await conn.QueryAsync<Row>(sql, p);

            var list = new List<UserDto>();
            foreach (var r in rows)
            {
                list.Add(new UserDto(r.Id, r.Username, r.Email, r.Roles ?? Array.Empty<string>(), r.CreatedUtc, r.UpdatedUtc));
            }
            return list;
        }

        private sealed class Row
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = default!;
            public string Email { get; set; } = default!;
            public DateTime CreatedUtc { get; set; }
            public DateTime UpdatedUtc { get; set; }
            public string[]? Roles { get; set; }
        }
    }
}
