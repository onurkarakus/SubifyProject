using FluentValidation;

namespace Subify.Application.Features.Auth.Logout;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AllSessions || !string.IsNullOrWhiteSpace(x.RefreshToken))
            .WithMessage("Provide refreshToken, or set allSessions to true while authenticated.");
    }
}
