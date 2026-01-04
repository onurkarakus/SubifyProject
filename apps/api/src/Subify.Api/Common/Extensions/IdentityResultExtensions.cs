using Microsoft.AspNetCore.Identity;
using Subify.Domain.Shared;

namespace Subify.Api.Common.Extensions;

public static class IdentityResultExtensions
{
    public static List<Error> GetErrors(this IdentityResult result)
    {
        // Identity hatalarını bizim Domain Error yapısına çeviriyoruz
        return result.Errors.Select(e => new Error(
            e.Code,
            "Identity Error",
            e.Description,
            ErrorType.Validation 
        )).ToList();
    }
}