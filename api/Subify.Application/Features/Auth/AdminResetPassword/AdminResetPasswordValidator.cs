using FluentValidation;
using Subify.Application.Common.Validation;

namespace Subify.Application.Features.Auth.AdminResetPassword;

public sealed class AdminResetPasswordValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.NewPassword).ApplySubifyPasswordRules();
    }
}
