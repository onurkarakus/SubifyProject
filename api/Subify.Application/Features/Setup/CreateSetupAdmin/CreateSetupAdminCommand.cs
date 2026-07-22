using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Identity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Extensions;
using Subify.Domain.Common;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.CreateSetupAdmin;

/// <summary>
/// First SuperAdmin via setup (tasks 3.3.1 / 3.3.6). Not available after setup complete.
/// </summary>
public sealed record CreateSetupAdminCommand(
    string FullName,
    string Email,
    string Password) : IRequest<Result<CreateSetupAdminResponse>>;

public sealed record CreateSetupAdminResponse(
    string UserId,
    string Email,
    string FullName,
    string Role);

public sealed class CreateSetupAdminHandler
    : IRequestHandler<CreateSetupAdminCommand, Result<CreateSetupAdminResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubifyDbContext _db;

    public CreateSetupAdminHandler(UserManager<ApplicationUser> userManager, ISubifyDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<Result<CreateSetupAdminResponse>> Handle(
        CreateSetupAdminCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is { IsSetupComplete: true })
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Setup.AlreadyComplete);
        }

        if (await SuperAdminBootstrap.HasAnySuperAdminAsync(_userManager))
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.SuperAdminAlreadyExists);
        }

        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser { Id = GuidGenerator.NewId() };
        user.ApplyRegistrationProfile(fullName, email);
        user.EmailConfirmed = true;

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            if (create.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<CreateSetupAdminResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<CreateSetupAdminResponse>(create.GetErrors());
        }

        var roleAssign = await SuperAdminBootstrap.TryAssignFirstSuperAdminAsync(_userManager, user);
        if (roleAssign.IsFailure)
        {
            // Cleanup orphaned user if race lost
            if (roleAssign.Error.Code == DomainErrors.Auth.SuperAdminBootstrapRace.Code
                || roleAssign.Error.Code == DomainErrors.Auth.SuperAdminAlreadyExists.Code)
            {
                await _userManager.DeleteAsync(user);
            }

            return Result.Failure<CreateSetupAdminResponse>(roleAssign.Error);
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new CreateSetupAdminResponse(
            UserId: user.Id.ToString(),
            Email: user.Email!,
            FullName: user.FullName,
            Role: Domain.Constants.AppRoles.SuperAdmin));
    }
}
