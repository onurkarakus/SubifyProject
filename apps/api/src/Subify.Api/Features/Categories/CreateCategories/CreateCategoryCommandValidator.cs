namespace Subify.Api.Features.Categories.CreateCategories;

using FluentValidation;
using Subify.Api.Common.Extensions; // Assuming ErrorCode extension is here

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithErrorCode("VAL_002").WithMessage("Category slug is required.")
            .MaximumLength(50).WithErrorCode("VAL_004").WithMessage("Category slug cannot exceed 50 characters.")
            .Matches("^[a-z0-9-]+$").WithErrorCode("VAL_003").WithMessage("Category slug must be lowercase, alphanumeric, and can contain hyphens.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithErrorCode("VAL_002").WithMessage("Category icon is required.")
            .MaximumLength(50).WithErrorCode("VAL_004").WithMessage("Category icon cannot exceed 50 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithErrorCode("VAL_002").WithMessage("Category color is required.")
            .MaximumLength(10).WithErrorCode("VAL_004").WithMessage("Category color cannot exceed 10 characters.")
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithErrorCode("VAL_003").WithMessage("Category color must be a valid hex code (e.g., #RRGGBB or #RGB).");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithErrorCode("VAL_003").WithMessage("Sort order must be a non-negative integer.");

        RuleFor(x => x.NameTR)
            .NotEmpty().WithErrorCode("VAL_002").WithMessage("Turkish category name is required.")
            .MaximumLength(100).WithErrorCode("VAL_004").WithMessage("Turkish category name cannot exceed 100 characters.");

        RuleFor(x => x.NameEN)
            .NotEmpty().WithErrorCode("VAL_002").WithMessage("English category name is required.")
            .MaximumLength(100).WithErrorCode("VAL_004").WithMessage("English category name cannot exceed 100 characters.");
    }
}