namespace Subify.Api.Features.Categories.CreateCategory;

using FluentValidation;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.NameTR).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEN).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(50).Matches("^[a-z0-9-]+$").WithMessage("Slug sadece küçük harf, rakam ve tire içerebilir.");
        RuleFor(x => x.Icon).NotEmpty();
        RuleFor(x => x.Color).NotEmpty().Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithMessage("Geçerli bir renk kodu giriniz.");
    }
}