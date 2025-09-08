using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using MongoDB.Driver;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Infrastructure.Persistence;
using EnterpriseBoilerplate.Infrastructure.Persistence.Repositories;
using EnterpriseBoilerplate.Infrastructure.ReadModel;
using EnterpriseBoilerplate.Infrastructure.Caching;
using EnterpriseBoilerplate.Infrastructure.Auth;
using EnterpriseBoilerplate.Infrastructure.Messaging;

namespace EnterpriseBoilerplate.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var writeConn = config.GetConnectionString("WriteDb") ?? "Host=localhost;Port=5432;Database=appdb;Username=app;Password=app;";
            services.AddDbContext<WriteDbContext>(opt => opt.UseNpgsql(writeConn));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            // Read side
            services.AddScoped<IReadDbConnectionFactory, NpgsqlReadDbConnectionFactory>();
            services.AddScoped<IUserReadRepository, UserReadRepository>();

            // Redis
            var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<ICacheService, RedisCacheService>();

            // Mongo
            var mongoConn = config.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
            var mongoDbName = config["Mongo:Database"] ?? "app_read";
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));

            // Auth
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            // EventBus
            services.AddSingleton<IEventBus, RabbitMqEventBus>();

            return services;
        }
    }
}
