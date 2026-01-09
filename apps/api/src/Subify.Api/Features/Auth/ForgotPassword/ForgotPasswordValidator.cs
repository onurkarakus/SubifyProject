using FluentValidation;

namespace Subify.Api.Features.Auth.ForgotPassword;

public class ForgotPasswordValidator: AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");
    }
}
