using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;
using Subify.Domain.Errors;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public VerifyEmailHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Result.Failure(DomainErrors.UserErrors.NotFound);
        }

        var decodedToken = System.Web.HttpUtility.UrlDecode(request.Token);

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            return Result.Failure(IdentityResultExtensions.GetErrors(result));
        }

        return Result.Success();
    }
}