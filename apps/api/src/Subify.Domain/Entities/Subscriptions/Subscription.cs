using Subify.Domain.Abstractions.Common;
using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;
using Subify.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Entities.Subscriptions;

public class Subscription : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }

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

    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<PaymentRecord> PaymentRecords { get; set; } = [];
    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
}
