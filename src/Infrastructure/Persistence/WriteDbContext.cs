using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseBoilerplate.Domain.Common;
using EnterpriseBoilerplate.Domain.Users;
using EnterpriseBoilerplate.Domain.Users.Events;

namespace EnterpriseBoilerplate.Infrastructure.Persistence
{
    public sealed class WriteDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasDefaultSchema(null);
            b.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Username).HasMaxLength(50).IsRequired();
                e.Property(x => x.Email).HasMaxLength(200).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.CreatedUtc).IsRequired();
                e.Property(x => x.UpdatedUtc).IsRequired();

                // Roles به عنوان مجموعه Owned
                e.OwnsMany<UserRole>("_roles", rb =>
                {
                    rb.ToTable("UserRoles");
                    rb.WithOwner().HasForeignKey("UserId");
                    rb.Property(r => r.Name).HasColumnName("Role").HasMaxLength(50).IsRequired();
                    rb.HasKey("UserId", "Name");
                });
            });

            b.Entity<OutboxMessage>(e =>
            {
                e.ToTable("OutboxMessages");
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasMaxLength(200).IsRequired();
                e.Property(x => x.Payload).IsRequired();
                e.Property(x => x.OccurredOn).IsRequired();
                e.Property(x => x.ProcessedOn);
                e.Property(x => x.Error);
            });

            b.Entity<InboxMessage>(e =>
            {
                e.ToTable("InboxMessages");
                e.HasKey(x => x.Id);
                e.Property(x => x.ProcessedOn).IsRequired();
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            AddOutboxMessages();
            return await base.SaveChangesAsync(ct);
        }

        private void AddOutboxMessages()
        {
            var entities = ChangeTracker
                .Entries<Entity<System.Guid>>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToArray();

            var events = entities.SelectMany(e => e.DomainEvents).ToArray();

            foreach (var evt in events)
            {
                var (type, payload) = MapEvent(evt);
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = System.Guid.NewGuid(),
                    Type = type,
                    Payload = payload,
                    OccurredOn = evt.OccurredOn
                });
            }

            foreach (var e in entities) e.ClearDomainEvents();
        }

        private static (string type, string payload) MapEvent(IDomainEvent evt)
        {
            switch (evt)
            {
                case UserRegisteredDomainEvent e:
                    return ("user.registered", JsonSerializer.Serialize(new { e.UserId, e.Username, e.Email, e.OccurredOn }));
                case UserUpdatedDomainEvent e:
                    return ("user.updated", JsonSerializer.Serialize(new { e.UserId, e.OccurredOn }));
                case UserRoleAssignedDomainEvent e:
                    return ("user.role_assigned", JsonSerializer.Serialize(new { e.UserId, e.Role, e.OccurredOn }));
                case UserDeletedDomainEvent e:
                    return ("user.deleted", JsonSerializer.Serialize(new { e.UserId, e.OccurredOn }));
                default:
                    return (evt.GetType().Name, JsonSerializer.Serialize(evt, evt.GetType()));
            }
        }
    }
}
