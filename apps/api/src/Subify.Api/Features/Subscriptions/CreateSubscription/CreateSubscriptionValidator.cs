using FluentValidation;

namespace Subify.Api.Features.Subscriptions.CreateSubscription;

public class CreateSubscriptionValidator: AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Tutar 0'dan büyük olmalıdır.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Para birimi 3 karakter olmalıdır (Örn: TRY).");
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.ReminderDaysBefore).GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.CategoryId.HasValue || x.UserCategoryId.HasValue)
            .WithMessage("Lütfen bir kategori seçiniz.");
                
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => !x.ProviderId.HasValue)
            .WithMessage("Sağlayıcı seçmediyseniz bir isim girmelisiniz.");
    }
}
