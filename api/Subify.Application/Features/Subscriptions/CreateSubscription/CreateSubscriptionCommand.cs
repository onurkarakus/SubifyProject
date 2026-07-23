using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.CreateSubscription;

/// <summary>
/// Create a subscription for the current user (4.1.1). No freemium count limit.
/// </summary>
public sealed record CreateSubscriptionCommand(
    string Name,
    decimal Price,
    string Currency,
    string BillingCycle,
    int SharedWithCount,
    DateOnly NextRenewalDate,
    Guid? ProviderId = null,
    Guid? CategoryId = null,
    Guid? UserCategoryId = null,
    DateOnly? LastUsedAt = null,
    string? Notes = null) : IRequest<Result<CreateSubscriptionResponse>>;
