using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.UpdateSubscription;

/// <summary>Update own subscription (4.1.6). Ownership required; activity old/new values.</summary>
public sealed record UpdateSubscriptionCommand(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string BillingCycle,
    int SharedWithCount,
    DateOnly NextRenewalDate,
    Guid? ProviderId = null,
    Guid? CategoryId = null,
    Guid? UserCategoryId = null,
    string? Notes = null) : IRequest<Result<SubscriptionResponse>>;
