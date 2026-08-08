using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Users.PatchAdminUser;

/// <summary>
/// SuperAdmin: lock/unlock, soft-disable, role Admin/User (7.1.3 / 7.1.5).
/// SuperAdmin target accounts are protected (USER_004).
/// </summary>
public sealed record PatchAdminUserCommand(
    Guid UserId,
    string? Role = null,
    bool? IsLocked = null,
    bool? IsDisabled = null,
    string? FullName = null) : IRequest<Result<AdminUserResponse>>;

public sealed class PatchAdminUserValidator : AbstractValidator<PatchAdminUserCommand>
{
    public PatchAdminUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role!)
            .Must(AdminUserAccess.IsAssignableRole)
            .WithMessage("Role must be User or Admin.")
            .When(x => !string.IsNullOrWhiteSpace(x.Role));
        RuleFor(x => x.FullName)
            .MaximumLength(UserProfileConstants.FullNameMaxLength)
            .When(x => x.FullName is not null);
    }
}

public sealed class PatchAdminUserHandler
    : IRequestHandler<PatchAdminUserCommand, Result<AdminUserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public PatchAdminUserHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result<AdminUserResponse>> Handle(
        PatchAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var access = AdminUserAccess.RequireSuperAdmin(_currentUser);
        if (access.IsFailure)
        {
            return Result.Failure<AdminUserResponse>(access.Error);
        }

        if (request is { Role: null, IsLocked: null, IsDisabled: null, FullName: null })
        {
            return Result.Failure<AdminUserResponse>(Error.Validation(
                "USER_010",
                "No Changes",
                "Provide at least one of: role, isLocked, isDisabled, fullName."));
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.UserErrors.NotFound);
        }

        var targetRoles = await _userManager.GetRolesAsync(user);
        var targetIsSuperAdmin = targetRoles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

        // SuperAdmin accounts cannot be locked, disabled, or re-roled via this endpoint.
        if (targetIsSuperAdmin
            && (request.Role is not null || request.IsLocked is not null || request.IsDisabled is not null))
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.UserErrors.CannotModifySuperAdmin);
        }

        if (_currentUser.UserId == user.Id
            && (request.IsLocked is true || request.IsDisabled is true))
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.UserErrors.CannotDisableSelf);
        }

        if (_currentUser.UserId == user.Id && request.Role is not null)
        {
            return Result.Failure<AdminUserResponse>(DomainErrors.UserErrors.CannotChangeOwnRole);
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.UpdateProfile(fullName: request.FullName);
        }

        if (request.Role is not null)
        {
            var desired = AdminUserAccess.NormalizeAssignableRole(request.Role);
            var currentAssignable = targetRoles
                .Where(r => AdminUserAccess.IsAssignableRole(r))
                .ToList();

            foreach (var r in currentAssignable)
            {
                if (!string.Equals(r, desired, StringComparison.OrdinalIgnoreCase))
                {
                    var remove = await _userManager.RemoveFromRoleAsync(user, r);
                    if (!remove.Succeeded)
                    {
                        return Result.Failure<AdminUserResponse>(
                            Error.Failure("USER_011", "Role Update Failed", remove.Errors.First().Description));
                    }
                }
            }

            if (!await _userManager.IsInRoleAsync(user, desired))
            {
                var add = await _userManager.AddToRoleAsync(user, desired);
                if (!add.Succeeded)
                {
                    return Result.Failure<AdminUserResponse>(
                        Error.Failure("USER_011", "Role Update Failed", add.Errors.First().Description));
                }
            }
        }

        if (request.IsLocked is true)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            // Permanent admin lock (distinct from short failed-attempt lockout)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await RevokeSessionsAsync(user.Id, "admin_lock", cancellationToken);
        }
        else if (request.IsLocked is false)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        if (request.IsDisabled is true)
        {
            user.Disable();
            await RevokeSessionsAsync(user.Id, "admin_disable", cancellationToken);
        }
        else if (request.IsDisabled is false)
        {
            user.Enable();
        }

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure<AdminUserResponse>(
                Error.Failure("USER_012", "User Update Failed", update.Errors.First().Description));
        }

        var subCount = await _db.Subscriptions
            .AsNoTracking()
            .CountAsync(
                s => s.UserId == user.Id && !s.Archived && s.DeletedAt == null,
                cancellationToken);

        // Reload for fresh lockout state
        user = (await _userManager.FindByIdAsync(user.Id.ToString()))!;
        return Result.Success(await AdminUserMapper.ToResponseAsync(
            _userManager, user, subCount, cancellationToken));
    }

    private async Task RevokeSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var sessions = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(RefreshToken.ReasonAdmin, reason);
        }

        if (sessions.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
