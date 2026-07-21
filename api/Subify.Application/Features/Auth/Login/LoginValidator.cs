using FluentValidation;

namespace Subify.Application.Features.Auth.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
        .NotEmpty().WithMessage("Email alanı zorunludur.")
        .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Şifre alanı zorunludur.");
    }
}