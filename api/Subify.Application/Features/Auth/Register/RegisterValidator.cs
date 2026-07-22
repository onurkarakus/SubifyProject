using FluentValidation;
using Subify.Application.Common.Validation;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(UserProfileConstants.FullNameMaxLength)
            .WithMessage($"Full name must be at most {UserProfileConstants.FullNameMaxLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256).WithMessage("Email is too long.");

        RuleFor(x => x.Password).ApplySubifyPasswordRules();
    }
}
