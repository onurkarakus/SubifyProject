using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Validation;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.ResetPasswordWithToken;

/// <summary>15.2.1 / 3.2.8 — complete password reset via email token.</summary>
public sealed record ResetPasswordWithTokenCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordWithTokenValidator : AbstractValidator<ResetPasswordWithTokenCommand>
{
    public ResetPasswordWithTokenValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.NewPassword).ApplySubifyPasswordRules();
    }
}

public sealed class ResetPasswordWithTokenHandler : IRequestHandler<ResetPasswordWithTokenCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubifyDbContext _db;

    public ResetPasswordWithTokenHandler(
        UserManager<ApplicationUser> userManager,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<Result> Handle(
        ResetPasswordWithTokenCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || user.IsDisabled)
        {
            return Result.Failure(DomainErrors.Auth.InvalidResetCode);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token.Trim(), request.NewPassword);
        if (!result.Succeeded)
        {
            // Identity returns InvalidToken etc. — map to AUTH_009
            return Result.Failure(DomainErrors.Auth.InvalidResetCode);
        }

        // Revoke sessions after password change
        var sessions = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(RefreshToken.ReasonAdmin, revokedByIp: "password_reset_email");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
