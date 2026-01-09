using FluentValidation;

namespace Subify.Api.Features.Auth.VerifyEmail;

public class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı ID gereklidir.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Doğrulama token'ı gereklidir.");
    }
}