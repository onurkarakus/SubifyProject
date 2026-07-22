using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Extensions;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Register;

/// <summary>
/// Public registration (tasks 3.2.1 / 3.2.11 / 3.2.13).
/// Assigns User role + default NotificationSettings.
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

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            if (createResult.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<RegisterResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<RegisterResponse>(createResult.GetErrors());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            return Result.Failure<RegisterResponse>(roleResult.GetErrors());
        }

        // Task 3.2.11 — notification defaults
        var hasSettings = await _db.NotificationSettings
            .AnyAsync(n => n.UserId == user.Id, cancellationToken);
        if (!hasSettings)
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
    /// Task 3.2.13: block public register until setup complete and AllowPublicRegistration.
    /// If no SystemSettings row yet, allow (dev bootstrap) unless explicitly seeded closed.
    /// When IsSetupComplete=false after settings exist, only setup wizard should create admin.
    /// </summary>
    private async Task<Result> EnsurePublicRegistrationAllowedAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            // First boot without seed race — allow
            return Result.Success();
        }

        if (!settings.IsSetupComplete)
        {
            // Setup incomplete: only first user may register (dev / pre-wizard).
            // After any user exists, further public register waits for setup (3S / 3.3).
            var anyUser = await _userManager.Users.AnyAsync(cancellationToken);
            return anyUser
                ? Result.Failure(DomainErrors.Auth.RegistrationDisabled)
                : Result.Success();
        }

        if (!settings.AllowPublicRegistration)
        {
            return Result.Failure(DomainErrors.Auth.RegistrationDisabled);
        }

        return Result.Success();
    }
}
