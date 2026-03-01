using Subify.Domain.Enums;

namespace Subify.Api.Features.Subscriptions.GetSubscriptions;

public record GetSubscriptionsResponse(
    Guid Id,
    Guid UserId,
    Guid? ProviderId,
    string ProviderName,
    Guid? CategoryId,
    string CategoryName,
    Guid? UserCategoryId,
    string UserCategoryName,
    string Name,
    string Description,
    string Icon,
    string Color,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateTime StartDate,
    DateOnly NextPaymentDate,
    bool RemindMe,
    int ReminderDaysBefore,
    SubscriptionStatus Status,
    DateTime CreatedAt,
    bool Archived
);
