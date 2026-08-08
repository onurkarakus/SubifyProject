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

namespace Subify.Application.Features.Auth.Register;

/// <summary>
/// Public registration (3.2.x / 3.3.2 / 3.3.6).
/// Always assigns <c>User</c> after setup. Blocked while setup incomplete (use setup admin).
/// </summary>
public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubifyDbContext _db;

    public RegisterHandler(UserManager<ApplicationUser> userManager, ISubifyDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var gate = await EnsurePublicRegistrationAllowedAsync(cancellationToken);
        if (gate.IsFailure)
        {
            return Result.Failure<RegisterResponse>(gate.Error);
        }

        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser
        {
            Id = GuidGenerator.NewId()
        };
        user.ApplyRegistrationProfile(fullName, email);
        user.EmailConfirmed = true;

        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            user.ApplyInstanceDefaults(
                locale: settings.DefaultLocale,
                mainCurrency: settings.DefaultCurrency,
                applicationThemeColor: settings.DefaultApplicationThemeColor,
                darkTheme: settings.DefaultDarkTheme);
        }

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            if (createResult.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<RegisterResponse>(createResult.GetErrors());
        }

        // Task 3.3.2 — public register is always User (never SuperAdmin)
        var roleResult = await SuperAdminBootstrap.AssignUserRoleAsync(_userManager, user);
        if (roleResult.IsFailure)
        {
            return Result.Failure<RegisterResponse>(roleResult.Error);
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new RegisterResponse(
            UserId: user.Id.ToString(),
            Email: user.Email!,
            FullName: user.FullName,
            Message: "Registration successful. You can sign in."));
    }

    /// <summary>
    /// 3.3.6: setup incomplete → no public register (SuperAdmin via /api/setup/admin).
    /// After setup: require AllowPublicRegistration.
    /// </summary>
    private async Task<Result> EnsurePublicRegistrationAllowedAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || !settings.IsSetupComplete)
        {
            return Result.Failure(DomainErrors.Auth.SetupRequired);
        }

        if (!settings.AllowPublicRegistration)
        {
            return Result.Failure(DomainErrors.Auth.RegistrationDisabled);
        }

        return Result.Success();
    }
}
