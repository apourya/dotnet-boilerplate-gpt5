using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseBoilerplate.Application.Common.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync(string topic, string messageType, string payload, string messageId, CancellationToken ct = default);
    }
}
