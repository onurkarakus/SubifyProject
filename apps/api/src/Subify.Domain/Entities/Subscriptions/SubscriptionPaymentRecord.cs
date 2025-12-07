using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities.Subscriptions;

public class SubscriptionPaymentRecord: BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateOnly PaymentDate { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

}
