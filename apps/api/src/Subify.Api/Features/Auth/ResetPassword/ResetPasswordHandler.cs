using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Api.Common.Extensions;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            return Result.Failure(result.GetErrors());
        }

        return Result.Success();
    }
}
