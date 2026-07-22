using MediatR;
using Microsoft.AspNetCore.Identity;
using Subify.Application.Extensions;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var newUser = new ApplicationUser();
        newUser.ApplyRegistrationProfile(request.FullName, request.Email);

        var createResult = await _userManager.CreateAsync(newUser, request.Password);

        if (!createResult.Succeeded)
        {
            return Result.Failure<RegisterResponse>(createResult.GetErrors());
        }

        return Result.Success(new RegisterResponse
        (
            Email: newUser.Email!,
            UserId: newUser.Id.ToString(),
            Expiration: DateTime.UtcNow.AddMinutes(15),
            Message: "Kayıt başarılı."
        ));
    }
}
