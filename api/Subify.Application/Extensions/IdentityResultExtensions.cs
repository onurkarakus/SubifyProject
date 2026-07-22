using Microsoft.AspNetCore.Identity;
using Subify.Domain.Shared;

namespace Subify.Application.Extensions;

public static class IdentityResultExtensions
{
    public static bool IsDuplicateEmailOrUserName(this IdentityResult result) =>
        result.Errors.Any(e =>
            e.Code is "DuplicateUserName" or "DuplicateEmail"
                or "DuplicateUserNameNormalized" or "DuplicateEmailNormalized");

    public static List<Error> GetErrors(this IdentityResult result)
    {
        return result.Errors
            .Select(e => MapError(e))
            .ToList();
    }

    private static Error MapError(IdentityError error)
    {
        if (error.Code is "DuplicateUserName" or "DuplicateEmail"
            or "DuplicateUserNameNormalized" or "DuplicateEmailNormalized")
        {
            return Domain.Errors.DomainErrors.Auth.EmailAlreadyRegistered;
        }

        if (error.Code is "PasswordTooShort" or "PasswordRequiresDigit"
            or "PasswordRequiresLower" or "PasswordRequiresUpper"
            or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresUniqueChars")
        {
            return Domain.Errors.DomainErrors.Auth.PasswordTooWeak;
        }

        return Error.Validation(
            string.IsNullOrWhiteSpace(error.Code) ? "Identity.Error" : error.Code,
            "Identity Error",
            error.Description);
    }
}
