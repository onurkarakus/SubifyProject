using Subify.Domain.Abstractions.Common;
using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.AuditLogs;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.Subscriptions;

public sealed class Subscription : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? UserCategoryId { get; set; }

    /// <summary>
    /// Subscription name. Examples: 'Netflix', 'Spotify Premium'
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Price per billing cycle. 
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string Currency { get; set; } = "TRY";

    // <summary>
    /// Billing frequency. 
    /// </summary>
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    /// <summary>
    /// Next payment/renewal date.
    /// </summary>
    public DateOnly NextRenewalDate { get; set; }

    /// <summary>
    /// Last time user actively used this subscription.
    /// Used for AI "unused subscription" detection.
    /// </summary>
    public DateOnly? LastUsedAt { get; set; }

    /// <summary>
    /// User notes about the subscription.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Current status of the subscription.
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// Soft archive flag (UI hide, not delete).
    /// </summary>
    public bool Archived { get; set; }

    /// <summary>
    /// Total number of people sharing this subscription (including the user).
    /// 1 = not shared (user pays full price)
    /// 3 = shared with 2 others (user pays Price / 3)
    /// </summary>
    public int SharedWithCount { get; set; } = 1;

    /// <summary>
    /// Calculated: User's actual payment amount.
    /// Formula: Price / SharedWithCount
    /// This is a computed property, not stored in DB.
    /// </summary>
    public decimal UserShare => SharedWithCount > 0 ? Price / SharedWithCount : Price;

    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Category? Category { get; set; }
    public UserCategory? UserCategory { get; set; }
    public ICollection<SubscriptionPaymentRecord> PaymentRecords { get; set; } = [];
    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
}
