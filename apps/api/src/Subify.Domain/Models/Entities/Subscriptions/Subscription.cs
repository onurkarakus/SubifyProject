using Subify.Domain.Abstractions.Common;
using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.AuditLogs;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Providers;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.Subscriptions;

public sealed class Subscription : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }

    public Guid? ProviderId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? UserCategoryId { get; set; }
    
    /// <summary>
    /// Subscription name. Examples: 'Netflix', 'Spotify Premium'
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; }= string.Empty;

    public string? Icon { get; set; }

    public string? Color { get; set; }

    /// <summary>
    /// Amount per billing cycle. 
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string Currency { get; set; } = "TRY";

    // <summary>
    /// Billing frequency. 
    /// </summary>
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public DateTime StartDate { get; set; }

    /// <summary>
    /// Next payment/renewal date.
    /// </summary>
    public DateOnly NextPaymentDate { get; set; }

    public bool RemindMe { get; set; } = true;

    public int ReminderDaysBefore { get; set; } = 1;

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

    public UserCategory? UserCategory { get; set; }

    public Provider? Provider { get; set; }

    public ICollection<SubscriptionPaymentRecord> PaymentRecords { get; set; } = [];

    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
}
