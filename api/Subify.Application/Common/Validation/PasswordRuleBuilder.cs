using FluentValidation;
using Subify.Domain.Constants;

namespace Subify.Application.Common.Validation;

/// <summary>
/// Shared password rules aligned with <see cref="IdentitySecurityDefaults"/> (task 3.4.1).
/// </summary>
public static class PasswordRuleBuilder
{
    public static IRuleBuilderOptions<T, string> ApplySubifyPasswordRules<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(IdentitySecurityDefaults.PasswordMinLength)
            .WithMessage($"Password must be at least {IdentitySecurityDefaults.PasswordMinLength} characters.")
            .Matches(IdentitySecurityDefaults.PasswordUpperPattern)
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(IdentitySecurityDefaults.PasswordLowerPattern)
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(IdentitySecurityDefaults.PasswordDigitPattern)
            .WithMessage("Password must contain at least one digit.");
    }
}
