using FluentValidation;

namespace Subify.Api.Features.Auth.ResendConfirmation;

public class ResendConfirmationValidator: AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");
    }
}
