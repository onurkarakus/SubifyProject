using FluentValidation;
using Subify.Api.Features.Auth.Login;

namespace Subify.Api.Features.Auth.GetCurrentUser;

public class GetCurrentUserValidator: AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserValidator()
    {
        // Currently, there are no specific validation rules for GetCurrentUserQuery.
        // This class is defined for consistency and future extensibility.
    }
}
