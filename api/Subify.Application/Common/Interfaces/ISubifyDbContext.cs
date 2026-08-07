using Microsoft.EntityFrameworkCore;
using Subify.Domain.Entities;

namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Application-facing persistence abstraction (task 2.4.1).
/// Also the unit-of-work boundary for handlers (task 2.4.2) via <see cref="IUnitOfWork"/>.
/// Handlers depend on this interface — not <c>SubifyDbContext</c>.
/// </summary>
/// <remarks>
/// <para><b>Handler pattern:</b></para>
/// <code>
/// await _db.Subscriptions.AddAsync(entity, ct);
/// // more changes on tracked entities...
/// await _db.SaveChangesAsync(ct); // single commit
/// </code>
/// Avoid intermediate SaveChanges inside a multi-entity use case unless a mid-flow
/// transaction boundary is explicitly required.
/// </remarks>
public interface ISubifyDbContext : IUnitOfWork
{
    // --- Identity / auth ---
    DbSet<ApplicationUser> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserInvite> UserInvites { get; }
    DbSet<UserDeviceToken> UserDeviceTokens { get; }

    // --- Core catalog & subscriptions ---
    DbSet<Category> Categories { get; }
    DbSet<UserCategory> UserCategories { get; }
    DbSet<Provider> Providers { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionPriceHistory> SubscriptionPriceHistories { get; }

    // --- Instance / i18n / notifications ---
    DbSet<SystemSettings> SystemSettings { get; }
    DbSet<Resource> Resources { get; }
    DbSet<NotificationSetting> NotificationSettings { get; }
    DbSet<EmailTemplates> EmailTemplates { get; }
    DbSet<EmailSendLog> EmailSendLogs { get; }

    // --- Logs / FX ---
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<AiSuggestionLog> AISuggestionLogs { get; }
    DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots { get; }

    /// <summary>
    /// Convenience for auth token rotation: add refresh token and <b>immediately</b> commit.
    /// For multi-entity units of work, prefer <see cref="RefreshTokens"/> + one
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> at the end instead.
    /// </summary>
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
