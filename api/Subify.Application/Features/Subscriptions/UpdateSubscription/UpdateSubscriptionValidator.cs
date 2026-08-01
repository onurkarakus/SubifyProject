using FluentValidation;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Subscriptions.UpdateSubscription;

public sealed class UpdateSubscriptionValidator : AbstractValidator<UpdateSubscriptionCommand>
{
    public UpdateSubscriptionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(SubscriptionConstants.NameMaxLength);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(SubscriptionConstants.CurrencyMaxLength)
            .Must(SupportedCurrencies.IsSupported)
            .WithMessage("Currency must be a supported ISO code (TRY, USD, EUR, GBP).");

        RuleFor(x => x.BillingCycle)
            .NotEmpty()
            .Must(CreateSubscriptionValidator.BeSupportedBillingCycle)
            .WithMessage("Billing cycle must be 'monthly' or 'yearly'.");

        RuleFor(x => x.SharedWithCount)
            .GreaterThanOrEqualTo(SubscriptionConstants.MinSharedWithCount);

        RuleFor(x => x.Notes)
            .MaximumLength(SubscriptionConstants.NotesMaxLength)
            .When(x => x.Notes is not null);

        RuleFor(x => x)
            .Must(x => !(x.CategoryId.HasValue && x.UserCategoryId.HasValue))
            .WithMessage("Cannot set both categoryId and userCategoryId.");
    }
}
