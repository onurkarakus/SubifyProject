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
/// SuperAdmin password reset for another user (task 3.2.15). No email required.
/// </summary>
public sealed class AdminResetPasswordHandler : IRequestHandler<AdminResetPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public AdminResetPasswordHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        if (_currentUser.UserId == request.UserId)
        {
            return Result.Failure(Error.Failure(
                "USER_005",
                "Use Change Password",
                "Use change-password for your own account."));
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(DomainErrors.UserErrors.NotFound);
        }

        // Protect SuperAdmin from non-self reset by another SuperAdmin? Allow for self-host recovery.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
        {
            return Result.Failure(reset.GetErrors());
        }

        var sessions = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(RefreshToken.ReasonAdmin, "admin_reset_password");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
