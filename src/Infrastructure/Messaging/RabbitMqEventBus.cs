using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseBoilerplate.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Polly;
using RabbitMQ.Client;

namespace EnterpriseBoilerplate.Infrastructure.Messaging
{
    public sealed class RabbitMqEventBus : IEventBus, IDisposable
    {
        private IConnection? _conn;
        private IChannel? _channel;
        private readonly Uri _uri;
        private readonly string _exchange;

        public RabbitMqEventBus(IConfiguration config)
        {
            _uri = new Uri(config["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/");
            _exchange = config["RabbitMQ:Exchange"] ?? "domain-events";
        }

        private async Task EnsureChannelAsync(CancellationToken ct)
        {
            if (_channel is { IsOpen: true }) return;

            if (!(_conn is { IsOpen: true }))
            {
                var factory = new ConnectionFactory { Uri = _uri };
                _conn = await factory.CreateConnectionAsync().ConfigureAwait(false);
            }

            _channel = await _conn!.CreateChannelAsync().ConfigureAwait(false);
            await _channel.ExchangeDeclareAsync(_exchange, "topic", durable: true).ConfigureAwait(false);
        }

        public async Task PublishAsync(string topic, string messageType, string payload, string messageId, CancellationToken ct = default)
        {
            await EnsureChannelAsync(ct).ConfigureAwait(false);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = messageId,
                Type = messageType
            };

            var body = Encoding.UTF8.GetBytes(payload);

            var retry = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * attempt));

            await retry.ExecuteAsync(async () =>
            {
                await _channel!.BasicPublishAsync(
                    exchange: _exchange,
                    routingKey: messageType,
                    mandatory: false,
                    basicProperties: props,
                    body: body
                ).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            try { _channel?.CloseAsync().GetAwaiter().GetResult(); } catch { }
            _channel?.Dispose();
            try { _conn?.CloseAsync().GetAwaiter().GetResult(); } catch { }
            _conn?.Dispose();
        }
    }
}