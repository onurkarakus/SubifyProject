using Microsoft.AspNetCore.Identity;
using Subify.Domain.Abstractions.Common;
using Subify.Domain.Entities.Subscriptions;

namespace Subify.Domain.Entities.Users;

public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    // Audit fields (manual)
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public Profile? Profile { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<PushToken> PushTokens { get; set; } = [];
    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
    public ICollection<AiSuggestionLog> AiSuggestionLogs { get; set; } = [];
    public ICollection<BillingSession> BillingSessions { get; set; } = [];
    public ICollection<EntitlementCache> Entitlements { get; set; } = [];
}
