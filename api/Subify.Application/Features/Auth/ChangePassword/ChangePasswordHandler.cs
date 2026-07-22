using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Extensions;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.ChangePassword;

/// <summary>Change own password and revoke other refresh sessions (task 3.2.14).</summary>
public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public ChangePasswordHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(DomainErrors.UserErrors.NotFound);
        }

        var change = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!change.Succeeded)
        {
            if (change.Errors.Any(e => e.Code is "PasswordMismatch"))
            {
                return Result.Failure(DomainErrors.Auth.InvalidCredentials);
            }

            return Result.Failure(change.GetErrors());
        }

        // Invalidate all refresh sessions after password change
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(RefreshToken.ReasonLogout, "password_change");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
