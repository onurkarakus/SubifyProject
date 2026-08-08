using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Validation;
using Subify.Application.Extensions;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Users.CreateAdminUser;

/// <summary>
/// Manually create a user (7.1.2). SuperAdmin or Admin.
/// Role: User (default) or Admin only — never SuperAdmin.
/// </summary>
public sealed record CreateAdminUserCommand(
    string Email,
    string FullName,
    string Password,
    string? Role = null) : IRequest<Result<AdminUserResponse>>;

public sealed class CreateAdminUserValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(UserProfileConstants.FullNameMaxLength);

        RuleFor(x => x.Password).ApplySubifyPasswordRules();

        RuleFor(x => x.Role!)
            .Must(AdminUserAccess.IsAssignableRole)
            .WithMessage("Role must be User or Admin.")
            .When(x => !string.IsNullOrWhiteSpace(x.Role));
    }
}

public sealed class CreateAdminUserHandler
    : IRequestHandler<CreateAdminUserCommand, Result<AdminUserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public CreateAdminUserHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result<AdminUserResponse>> Handle(
        CreateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var access = AdminUserAccess.RequireAdminOrAbove(_currentUser);
        if (access.IsFailure)
        {
            return Result.Failure<AdminUserResponse>(access.Error);
        }

        var role = string.IsNullOrWhiteSpace(request.Role)
            ? AppRoles.User
            : AdminUserAccess.NormalizeAssignableRole(request.Role);

        // Only SuperAdmin may create Admin accounts
        if (string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            && !AdminUserAccess.IsSuperAdmin(_currentUser))
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.UserErrors.AccessDenied);
        }

        var email = request.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser { Id = GuidGenerator.NewId() };
        user.ApplyRegistrationProfile(request.FullName, email);
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

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            if (create.IsDuplicateEmailOrUserName())
            {
                return Result.Failure<AdminUserResponse>(DomainErrors.Auth.EmailAlreadyRegistered);
            }

            return Result.Failure<AdminUserResponse>(create.GetErrors());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return Result.Failure<AdminUserResponse>(roleResult.GetErrors());
        }

        if (!await _db.NotificationSettings.AnyAsync(n => n.UserId == user.Id, cancellationToken))
        {
            await _db.NotificationSettings.AddAsync(
                NotificationSetting.CreateDefaults(user.Id),
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(await AdminUserMapper.ToResponseAsync(
            _userManager, user, activeSubscriptionCount: 0, cancellationToken));
    }
}
