using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Users;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> userManager;

    public LoginHandler(UserManager<ApplicationUser> userManager)
    {
        this.userManager = userManager;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
        }

        var generateTokenResult = await GenerateTokensAsync(user);

        if (!generateTokenResult.IsSuccess)
        {
            return Result.Failure<LoginResponse>(generateTokenResult.Errors);
        }

        return Result.Success(new LoginResponse
        (
            email: user.Email,
            accessToken: generateTokenResult.Value.accessToken,
            refreshToken: generateTokenResult.Value.refreshToken,
            expiration: generateTokenResult.Value.expiration
        ));
    }
}
