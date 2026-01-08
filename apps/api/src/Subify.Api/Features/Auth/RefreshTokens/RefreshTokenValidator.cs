using FluentValidation;

namespace Subify.Api.Features.Auth.RefreshTokens;

public class RefreshTokenValidator: AbstractValidator<RefreshTokensCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");

        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required.");
    }
}
