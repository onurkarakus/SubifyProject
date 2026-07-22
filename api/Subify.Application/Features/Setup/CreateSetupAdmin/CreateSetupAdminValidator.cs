using FluentValidation;
using Subify.Application.Common.Validation;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Setup.CreateSetupAdmin;

public sealed class CreateSetupAdminValidator : AbstractValidator<CreateSetupAdminCommand>
{
    public CreateSetupAdminValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(UserProfileConstants.FullNameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password).ApplySubifyPasswordRules();
    }
}
