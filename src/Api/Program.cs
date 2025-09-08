using System.Text;
using EnterpriseBoilerplate.Application;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Application.Users;
using EnterpriseBoilerplate.Application.Users.Commands;
using EnterpriseBoilerplate.Application.Users.Queries;
using EnterpriseBoilerplate.Infrastructure;
using EnterpriseBoilerplate.Infrastructure.Persistence;
using HealthChecks.RabbitMQ;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Prometheus;
using RabbitMQ.Client;
using MongoDB.Driver;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext();
});

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EnterpriseBoilerplate API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme{ Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] { }
        }
    });
});

var key = builder.Configuration["Auth:Jwt:Key"] ?? "insecure-dev-key-change-me";
var issuer = builder.Configuration["Auth:Jwt:Issuer"] ?? "EnterpriseBoilerplate";
var audience = builder.Configuration["Auth:Jwt:Audience"] ?? "EnterpriseBoilerplate.Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddHttpClient("default")
    .AddResilienceHandler("standard", (pipeline, ctx) =>
    {
        pipeline.AddRetry(new() { MaxRetryAttempts = 3, UseJitter = true });
        pipeline.AddCircuitBreaker(new() { FailureRatio = 0.5, SamplingDuration = TimeSpan.FromSeconds(30), MinimumThroughput = 10, BreakDuration = TimeSpan.FromSeconds(15) });
        pipeline.AddTimeout(TimeSpan.FromSeconds(10));
    });

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("WriteDb")!, name: "postgres")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis")
    .AddRabbitMQ(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var factory = new global::RabbitMQ.Client.ConnectionFactory
        {
            Uri = new Uri(cfg["RabbitMQ:ConnectionString"]!)
        };
        // برای اطمینان از امضا، یک نام کلاینت هم می‌دهیم
        return factory.CreateConnectionAsync("healthcheck");
    })
    .AddMongoDb(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var cs = cfg.GetConnectionString("Mongo")!;
        return new MongoClient(cs);
    }, name: "mongo");

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseHttpMetrics(); // Prometheus

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/swagger"));

// Auth endpoints
app.MapPost("/api/auth/register", async (RegisterUserCommand cmd, MediatR.IMediator mediator) =>
{
    var dto = await mediator.Send(cmd);
    return Results.Ok(dto);
});

app.MapPost("/api/auth/login", async (LoginUserQuery q, MediatR.IMediator mediator) =>
{
    var auth = await mediator.Send(q);
    return Results.Ok(auth);
});

// Users endpoints
app.MapGet("/api/users/{id:guid}", [Authorize] async (Guid id, MediatR.IMediator mediator) =>
{
    var dto = await mediator.Send(new GetUserByIdQuery(id));
    return Results.Ok(dto);
});

app.MapGet("/api/users", [Authorize(Policy = "AdminOnly")] async (DateTime? afterCreatedUtc, Guid? afterId, int pageSize, MediatR.IMediator mediator) =>
{
    var list = await mediator.Send(new ListUsersQuery(afterCreatedUtc, afterId, pageSize == 0 ? 50 : Math.Min(200, pageSize)));
    return Results.Ok(list);
});

app.MapPut("/api/users/{id:guid}", [Authorize] async (Guid id, UpdateUserCommand body, MediatR.IMediator mediator) =>
{
    if (id != body.Id) return Results.BadRequest("Id mismatch");
    var dto = await mediator.Send(body);
    return Results.Ok(dto);
});

app.MapPost("/api/users/{id:guid}/roles", [Authorize(Policy = "AdminOnly")] async (Guid id, string role, MediatR.IMediator mediator) =>
{
    var dto = await mediator.Send(new AssignRoleCommand(id, role));
    return Results.Ok(dto);
});

app.MapDelete("/api/users/{id:guid}", [Authorize(Policy = "AdminOnly")] async (Guid id, MediatR.IMediator mediator) =>
{
    await mediator.Send(new DeleteUserCommand(id));
    return Results.NoContent();
});

app.MapMetrics();

// Dev-only: EnsureCreated + seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    await db.Database.EnsureCreatedAsync();

    var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var admin = await repo.GetByUsernameAsync("admin");
    if (admin is null)
    {
        var user = EnterpriseBoilerplate.Domain.Users.User.Register("admin", "admin@local", hasher.Hash("Admin123!"));
        user.AssignRole("Admin");
        await repo.AddAsync(user);
        await uow.SaveChangesAsync();
    }
}

app.Run();
