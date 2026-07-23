using FluentValidation;
using Subify.Domain.Constants;
using Subify.Domain.Enums;

namespace Subify.Application.Features.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
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
            .Must(BeSupportedBillingCycle)
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

    internal static bool BeSupportedBillingCycle(string? value) =>
        TryParseBillingCycle(value, out _);

    internal static bool TryParseBillingCycle(string? value, out BillingCycle cycle)
    {
        cycle = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Enum.TryParse<BillingCycle>(value.Trim(), ignoreCase: true, out cycle)
            && Enum.IsDefined(cycle))
        {
            return true;
        }

        return false;
    }
}
