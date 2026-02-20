using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Abstractions.Common;
using Subify.Domain.Models.Entities.AI;
using Subify.Domain.Models.Entities.ApplicationPayments;
using Subify.Domain.Models.Entities.AuditLogs;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Notifications;
using Subify.Domain.Models.Entities.Providers;
using Subify.Domain.Models.Entities.Subscriptions;
using Subify.Domain.Models.Entities.Users;
using System.Reflection;

namespace Subify.Infrastructure.Persistence;

public class SubifyDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public SubifyDbContext(DbContextOptions<SubifyDbContext> options) : base(options)
    {
    }

    // Users & Auth
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();

    // Subscriptions
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UserCategory> UserCategories => Set<UserCategory>();
    public DbSet<SubscriptionPaymentRecord> SubscriptionPaymentRecords => Set<SubscriptionPaymentRecord>();

    //Providers
    public DbSet<Provider> Providers => Set<Provider>();

    // AI
    public DbSet<AiSuggestionLog> AIRequestLogs => Set<AiSuggestionLog>();

    // Billing
    public DbSet<BillingSession> BillingSessions => Set<BillingSession>();
    public DbSet<EntitlementCache> EntitlementCaches => Set<EntitlementCache>();

    // Notifications
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    // Common
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplySoftDeleteFilter(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAt = now;
                        baseEntity.UpdatedAt = now;
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedAt = now;
                        entry.Property(nameof(baseEntity.CreatedAt)).IsModified = false;
                        break;
                    default:
                        break;
                }
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeleteEntity)
            {
                entry.State = EntityState.Modified;

                softDeleteEntity.DeletedAt = now;

                if (entry.Entity is BaseEntity updatedBaseEntity)
                {
                    updatedBaseEntity.UpdatedAt = now;
                }
            }

            if (entry.Entity is ApplicationUser user)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        user.CreatedAt = now;
                        user.UpdatedAt = now;
                        break;
                    case EntityState.Modified:
                        user.UpdatedAt = now;
                        entry.Property(nameof(ApplicationUser.CreatedAt)).IsModified = false;
                        break;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDeleteFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                var nullCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?)));
                var lambda = System.Linq.Expressions.Expression.Lambda(nullCheck, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
