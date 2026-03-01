using MediatR;
using Subify.Domain.Enums;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Subscriptions.CreateSubscription;

public record CreateSubscriptionCommand(
    string Name,
    string? Decsription,
    Guid? ProviderId,
    Guid? CategoryId,
    Guid? UserCategoryId,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateTime StartDate,
    bool RemindMe,
    int ReminderDaysBefore,
    string? Icon,
    string? Color
) : IRequest<Result<Guid>>;

