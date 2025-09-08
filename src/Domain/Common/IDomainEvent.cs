using System;

namespace EnterpriseBoilerplate.Domain.Common
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
