using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Extensions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.AdminResetPassword;

/// <summary>
/// SuperAdmin password reset for another user (3.2.15 / 7.5.1).
/// No email. Revokes target sessions. Audits without storing the new password.
/// </summary>
public sealed class AdminResetPasswordHandler : IRequestHandler<AdminResetPasswordCommand, Result>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;
    private readonly IActivityLogger _activityLogger;

    public AdminResetPasswordHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db,
        IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.AccessDenied);
        }

        if (_currentUser.UserId == request.UserId)
        {
            return Result.Failure(DomainErrors.UserErrors.UseChangePassword);
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(DomainErrors.UserErrors.NotFound);
        }

        // Direct set (no email token): RemovePassword + AddPassword avoids reset-token plumbing.
        if (await _userManager.HasPasswordAsync(user))
        {
            var remove = await _userManager.RemovePasswordAsync(user);
            if (!remove.Succeeded)
            {
                return Result.Failure(remove.GetErrors());
            }
        }

        var add = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!add.Succeeded)
        {
            return Result.Failure(add.GetErrors());
        }

        // Clear temporary lockout so admin recovery works after failed logins
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        var sessions = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(RefreshToken.ReasonAdmin, "admin_reset_password");
        }

        // 7.5.1 audit — never include the new password
        var audit = JsonSerializer.Serialize(
            new
            {
                targetUserId = user.Id,
                targetEmail = user.Email,
                sessionsRevoked = sessions.Count
            },
            JsonOptions);

        await _activityLogger.LogAsync(
            userId: _currentUser.UserId.Value,
            entityType: ActivityLogConstants.EntityTypes.Auth,
            action: ActivityLogConstants.Actions.AdminPasswordReset,
            description: $"Password reset by admin for {user.Email}.",
            entityId: user.Id,
            oldValues: null,
            newValues: audit,
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
