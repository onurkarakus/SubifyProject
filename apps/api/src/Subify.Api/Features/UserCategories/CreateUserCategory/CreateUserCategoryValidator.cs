using FluentValidation;

namespace Subify.Api.Features.UserCategories.CreateUserCategory;

public class CreateUserCategoryValidator : AbstractValidator<CreateUserCategoryCommand>
{
    public CreateUserCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}