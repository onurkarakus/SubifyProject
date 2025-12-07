using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities.ApplicationPayments;

/// <summary>
/// Caches RevenueCat entitlements for fast premium checks.
/// Updated via webhook and reconciliation job.
/// </summary>
public sealed class EntitlementCache : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Entitlement identifier: 'premium'
    /// </summary>
    public string Entitlement { get; set; } = null!;

    /// <summary>
    /// Entitlement status. 
    /// </summary>
    public EntitlementStatus Status { get; set; } = EntitlementStatus.Active;

    /// <summary>
    /// When the entitlement expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Product ID from store.
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Store: 'app_store', 'play_store', 'stripe'
    /// </summary>
    public string? Store { get; set; }

    /// <summary>
    /// Is this a trial period. 
    /// </summary>
    public bool IsTrial { get; set; }

    /// <summary>
    /// Will auto-renew.
    /// </summary>
    public bool WillRenew { get; set; } = true;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}