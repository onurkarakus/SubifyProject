using FluentValidation;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Subscriptions.ListSubscriptions;

public sealed class ListSubscriptionsValidator : AbstractValidator<ListSubscriptionsQuery>
{
    public ListSubscriptionsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, SubscriptionConstants.MaxPageSize);

        RuleFor(x => x.Search)
            .MaximumLength(SubscriptionConstants.SearchMaxLength)
            .When(x => x.Search is not null);

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => x.Category is not null);

        RuleFor(x => x)
            .Must(x => !(x.CategoryId.HasValue && x.UserCategoryId.HasValue))
            .WithMessage("Cannot filter by both categoryId and userCategoryId.");
    }
}
