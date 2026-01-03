using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.ApplicationPayments;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.Subscriptions;

public sealed class SubscriptionPaymentRecord : BaseEntity
{
    public Guid? SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    public Guid? BillingSessionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateTimeOffset PaymentDate { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

    /// <summary>
    /// e.g. "card", "paypal", "revenuecat", "stripe"
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payment provider identifier (revenuecat/stripe) if any.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// External transaction id returned by provider.
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// External session id (checkout/session) from provider.
    /// </summary>
    public string? ProviderSessionId { get; set; }

    /// <summary>
    /// Raw provider response/payload (JSON) for debugging/audit.
    /// </summary>
    public string? ProviderPayload { get; set; }

    public bool IsRefunded { get; set; }

    public DateTimeOffset? RefundedAt { get; set; }

    public string? FailureReason { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Subscription? Subscription { get; set; } = null!;
    public BillingSession? BillingSession { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
