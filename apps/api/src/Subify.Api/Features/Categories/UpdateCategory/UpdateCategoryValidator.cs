using FluentValidation;

namespace Subify.Api.Features.Categories.UpdateCategories;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Icon).NotEmpty().WithMessage("İkon boş olamaz.");
        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("Geçerli bir HEX renk kodu giriniz (Örn: #FF0000).");
    }
}