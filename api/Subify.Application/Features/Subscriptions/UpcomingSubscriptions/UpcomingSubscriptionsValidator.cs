using FluentValidation;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Subscriptions.UpcomingSubscriptions;

public sealed class UpcomingSubscriptionsValidator : AbstractValidator<UpcomingSubscriptionsQuery>
{
    public UpcomingSubscriptionsValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(SubscriptionConstants.MinUpcomingDays, SubscriptionConstants.MaxUpcomingDays);
    }
}
