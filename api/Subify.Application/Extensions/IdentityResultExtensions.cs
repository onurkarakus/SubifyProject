using Microsoft.AspNetCore.Identity;
using Subify.Domain.Shared;


namespace Subify.Application.Extensions;

public static class IdentityResultExtensions
{
    public static List<Error> GetErrors(this IdentityResult result)
    {
        return [.. result.Errors.Select(e => new Error
        (
            e.Code,
            "Identity Error",
            e.Description,
            ErrorType.Validation
        ))];
    }
}