using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext and request-scoped unit of work (tasks 2.4.1 / 2.4.2).
/// All handler commits should go through <see cref="SaveChangesAsync"/> so GUID fill,
/// audit timestamps, and soft-delete conversion always run.
/// </summary>
public class SubifyDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, ISubifyDbContext
{
    public SubifyDbContext(DbContextOptions<SubifyDbContext> options) : base(options)
    {
    }

    // Users comes from IdentityDbContext<ApplicationUser, ...> and satisfies ISubifyDbContext.Users.

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UserCategory> UserCategories => Set<UserCategory>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPriceHistory> SubscriptionPriceHistories => Set<SubscriptionPriceHistory>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<AiSuggestionLog> AISuggestionLogs => Set<AiSuggestionLog>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<EmailTemplates> EmailTemplates => Set<EmailTemplates>();
    public DbSet<EmailSendLog> EmailSendLogs => Set<EmailSendLog>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();
    public DbSet<UserInvite> UserInvites => Set<UserInvite>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(SubifyDbContext).Assembly);

        // Task 2.1.10: client-generated UUID v7 (no DB NEWSEQUENTIALID / default)
        builder.ApplyClientGuidIdConvention();

        // Task 2.1.9: hide soft-deleted rows by default (DeletedAt != null)
        builder.ApplySoftDeleteQueryFilters();
    }

    /// <summary>
    /// Unit-of-work commit (task 2.4.2). Single entry for async persistence from Application.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareChangesForSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        PrepareChangesForSave();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Shared pre-save pipeline — only path that mutates tracked entities before SQL.
    /// </summary>
    private void PrepareChangesForSave()
    {
        this.AssignGuidIdsOnAdd();
        ApplyAuditTimestamps();
        ConvertHardDeletesToSoftDeletes();
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }

    /// <summary>
    /// Intercepts <see cref="EntityState.Deleted"/> for <see cref="ISoftDeletable"/> entities
    /// and converts them to a soft-delete (DeletedAt set) instead of a hard DELETE.
    /// Subscriptions also get Archived via <see cref="Subscription.Archive"/>.
    /// </summary>
    private void ConvertHardDeletesToSoftDeletes()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>().ToList())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;

            if (entry.Entity is Subscription subscription)
            {
                subscription.Archive();
            }
            else
            {
                entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
            }

            if (entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
