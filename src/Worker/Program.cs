using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBoilerplate.Application;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using EnterpriseBoilerplate.Infrastructure;
using EnterpriseBoilerplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog for HostApplicationBuilder
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hosted services
builder.Services.AddHostedService<OutboxPublisherHostedService>();
builder.Services.AddHostedService<UserProjectionConsumerHostedService>();

var host = builder.Build();
await host.RunAsync();

// -------------- Hosted Services --------------
public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IEventBus _bus;
    private readonly ILogger<OutboxPublisherHostedService> _logger;

    public OutboxPublisherHostedService(IServiceProvider sp, IEventBus bus, ILogger<OutboxPublisherHostedService> logger)
    {
        _sp = sp; _bus = bus; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();

                var batch = await db.OutboxMessages
                    .Where(x => x.ProcessedOn == null)
                    .OrderBy(x => x.OccurredOn)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var msg in batch)
                {
                    try
                    {
                        await _bus.PublishAsync("domain-events", msg.Type, msg.Payload, msg.Id.ToString(), stoppingToken);
                        msg.ProcessedOn = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.Error = ex.Message;
                        _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                    }
                }

                if (batch.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error");
            }

            await Task.Delay(1500, stoppingToken);
        }
    }
}

public sealed class UserProjectionConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<UserProjectionConsumerHostedService> _logger;

    private IConnection? _conn;
    private IChannel? _ch;

    public UserProjectionConsumerHostedService(IServiceProvider sp, IConfiguration config, ILogger<UserProjectionConsumerHostedService> logger)
    {
        _sp = sp; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var uri = _config["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/";
        var exchange = _config["RabbitMQ:Exchange"] ?? "domain-events";

        var factory = new ConnectionFactory { Uri = new Uri(uri) };

        _conn = await factory.CreateConnectionAsync().ConfigureAwait(false);
        _ch = await _conn.CreateChannelAsync().ConfigureAwait(false);

        await _ch.ExchangeDeclareAsync(exchange: exchange, type: "topic", durable: true).ConfigureAwait(false);

        var queueOk = await _ch.QueueDeclareAsync(
            queue: "projections-users",
            durable: true,
            exclusive: false,
            autoDelete: false
        ).ConfigureAwait(false);

        await _ch.QueueBindAsync(
            queue: queueOk.QueueName,
            exchange: exchange,
            routingKey: "user.*"
        ).ConfigureAwait(false);

        // اختیاری: محدودیت مصرف هم‌زمان
        // await _ch.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var messageId = ea.BasicProperties?.MessageId;
            if (string.IsNullOrWhiteSpace(messageId))
            {
                await _ch!.BasicAckAsync(ea.DeliveryTag, multiple: false).ConfigureAwait(false);
                return;
            }

            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();

                var inbox = await db.InboxMessages.FindAsync(
                    new object[] { Guid.Parse(messageId) },
                    cancellationToken: stoppingToken
                ).ConfigureAwait(false);

                if (inbox is not null)
                {
                    await _ch!.BasicAckAsync(ea.DeliveryTag, multiple: false).ConfigureAwait(false);
                    return; // already processed
                }

                var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
                await ProjectToMongoAsync(ea.RoutingKey, payload, scope, stoppingToken).ConfigureAwait(false);

                db.InboxMessages.Add(new InboxMessage { Id = Guid.Parse(messageId), ProcessedOn = DateTime.UtcNow });
                await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                await _ch!.BasicAckAsync(ea.DeliveryTag, multiple: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Projection consumer error");
                await _ch!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true).ConfigureAwait(false);
            }
        };

        await _ch.BasicConsumeAsync(queue: queueOk.QueueName, autoAck: false, consumer: consumer).ConfigureAwait(false);

        _logger.LogInformation("UserProjectionConsumer started");

        // نگه داشتن سرویس تا زمان توقف
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) { }
    }

    private static async Task ProjectToMongoAsync(string eventType, string json, IServiceScope scope, CancellationToken ct)
    {
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var users = mongo.GetCollection<UserRead>("users");

        switch (eventType)
        {
            case "user.registered":
                {
                    var e = JsonSerializer.Deserialize<UserRegistered>(json)!;
                    var filter = Builders<UserRead>.Filter.Eq(x => x.Id, e.UserId);
                    var doc = new UserRead
                    {
                        Id = e.UserId,
                        Username = e.Username,
                        Email = e.Email,
                        Roles = new[] { "User" },
                        UpdatedUtc = e.OccurredOn,
                        CreatedUtc = e.OccurredOn
                    };
                    await users.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
                    break;
                }
            case "user.updated":
                {
                    var e = JsonSerializer.Deserialize<UserUpdated>(json)!;
                    var filter = Builders<UserRead>.Filter.Eq(x => x.Id, e.UserId);
                    var update = Builders<UserRead>.Update.Set(x => x.UpdatedUtc, e.OccurredOn);
                    await users.UpdateOneAsync(filter, update, cancellationToken: ct);
                    break;
                }
            case "user.role_assigned":
                {
                    var e = JsonSerializer.Deserialize<UserRoleAssigned>(json)!;
                    var filter = Builders<UserRead>.Filter.Eq(x => x.Id, e.UserId);
                    var update = Builders<UserRead>.Update.AddToSet(x => x.Roles, e.Role).Set(x => x.UpdatedUtc, e.OccurredOn);
                    await users.UpdateOneAsync(filter, update, cancellationToken: ct);
                    break;
                }
            case "user.deleted":
                {
                    var e = JsonSerializer.Deserialize<UserDeleted>(json)!;
                    var filter = Builders<UserRead>.Filter.Eq(x => x.Id, e.UserId);
                    await users.DeleteOneAsync(filter, ct);
                    break;
                }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_ch is not null) await _ch.CloseAsync().ConfigureAwait(false); } catch { }
        _ch?.Dispose();
        try { if (_conn is not null) await _conn.CloseAsync().ConfigureAwait(false); } catch { }
        _conn?.Dispose();

        await base.StopAsync(cancellationToken);
    }

    private sealed record UserRegistered(Guid UserId, string Username, string Email, DateTime OccurredOn);
    private sealed record UserUpdated(Guid UserId, DateTime OccurredOn);
    private sealed record UserRoleAssigned(Guid UserId, string Role, DateTime OccurredOn);
    private sealed record UserDeleted(Guid UserId, DateTime OccurredOn);

    private sealed class UserRead
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}