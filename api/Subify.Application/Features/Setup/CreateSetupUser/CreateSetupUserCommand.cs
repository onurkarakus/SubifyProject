using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Validation;
using Subify.Application.Extensions;
using Subify.Application.Features.Admin.Users;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.CreateSetupUser;

/// <summary>
/// 3S.4.1 — create additional users during first-run setup (SuperAdmin, setup incomplete).
/// Same rules as admin create-user: role User|Admin, never SuperAdmin.
/// </summary>
public sealed record CreateSetupUserCommand(
    string Email,
    string FullName,
    string Password,
    string? Role = null) : IRequest<Result<SetupUserResponse>>;

public sealed record SetupUserResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public sealed class CreateSetupUserValidator : AbstractValidator<CreateSetupUserCommand>
{
    public CreateSetupUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(UserProfileConstants.FullNameMaxLength);

        RuleFor(x => x.Password).ApplySubifyPasswordRules();

        RuleFor(x => x.Role!)
            .Must(AdminUserAccess.IsAssignableRole)
            .WithMessage("Role must be User or Admin.")
            .When(x => !string.IsNullOrWhiteSpace(x.Role));
    }
}

public sealed class CreateSetupUserHandler
    : IRequestHandler<CreateSetupUserCommand, Result<SetupUserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public CreateSetupUserHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result<SetupUserResponse>> Handle(
        CreateSetupUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SetupUserResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<SetupUserResponse>(DomainErrors.UserErrors.AccessDenied);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure<SetupUserResponse>(DomainErrors.Setup.SettingsNotInitialized);
        }

        if (settings.IsSetupComplete)
        {
            return Result.Failure<SetupUserResponse>(DomainErrors.Setup.AlreadyComplete);
        }

        var role = string.IsNullOrWhiteSpace(request.Role)
            ? AppRoles.User
            : AdminUserAccess.NormalizeAssignableRole(request.Role);

        var email = request.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<SetupUserResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser { Id = GuidGenerator.NewId() };
        user.ApplyRegistrationProfile(request.FullName, email);
        user.EmailConfirmed = true;
        user.ApplyInstanceDefaults(
            locale: settings.DefaultLocale,
            mainCurrency: settings.DefaultCurrency,
            applicationThemeColor: settings.DefaultApplicationThemeColor,
            darkTheme: settings.DefaultDarkTheme);

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            if (create.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<SetupUserResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<SetupUserResponse>(create.GetErrors());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return Result.Failure<SetupUserResponse>(roleResult.GetErrors());
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new SetupUserResponse(
            Id: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: roles.ToList()));
    }
}
