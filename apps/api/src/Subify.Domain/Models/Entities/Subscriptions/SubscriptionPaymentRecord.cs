using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.Subscriptions;

public sealed class SubscriptionPaymentRecord: BaseEntity
{
    public Guid? SubscriptionId { get; set; }
    
    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateOnly PaymentDate { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public Subscription? Subscription { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}
