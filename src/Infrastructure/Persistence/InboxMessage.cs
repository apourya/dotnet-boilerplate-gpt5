using System;

namespace EnterpriseBoilerplate.Infrastructure.Persistence
{
    public sealed class InboxMessage
    {
        public Guid Id { get; set; }
        public DateTime ProcessedOn { get; set; }
    }
}
