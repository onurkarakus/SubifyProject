using Microsoft.AspNetCore.Identity;
using Subify.Domain.Abstractions.Common;
using Subify.Domain.Models.Entities.AI;
using Subify.Domain.Models.Entities.ApplicationPayments;
using Subify.Domain.Models.Entities.AuditLogs;
using Subify.Domain.Models.Entities.Auth;
using Subify.Domain.Models.Entities.Notifications;
using Subify.Domain.Models.Entities.Subscriptions;

namespace Subify.Domain.Models.Entities.Users;

public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;    

    public string? ReferralCode { get; set; }

    public bool MarketingOptIn { get; set; }

    // Audit fields (manual)
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation
    public Profile? Profile { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<PushToken> PushTokens { get; set; } = [];

    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];

    public ICollection<AiSuggestionLog> AiSuggestionLogs { get; set; } = [];

    public ICollection<BillingSession> BillingSessions { get; set; } = [];

    public ICollection<EntitlementCache> Entitlements { get; set; } = [];

    public ICollection<UserCategory> UserCategories { get; set; } = [];

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
