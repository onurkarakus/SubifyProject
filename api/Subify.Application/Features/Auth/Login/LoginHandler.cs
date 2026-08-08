using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Login;

/// <summary>
/// Email/password login (tasks 3.2.2 / 3.2.10). Issues tokens + user summary.
/// Does <b>not</b> require EmailConfirmed. Uses Identity lockout.
/// Logs successful login as activity (5.4.3).
/// </summary>
public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISubifyDbContext _dbContext;
    private readonly IActivityLogger _activityLogger;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ISubifyDbContext dbContext,
        IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _activityLogger = activityLogger;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
        }

        // 7.1.5 — soft-disabled accounts cannot sign in (distinct from temporary lockout)
        if (user.IsDisabled)
        {
            return Result.Failure<LoginResponse>(DomainErrors.UserErrors.AccountDisabled);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<LoginResponse>(BuildAccountLockedError(user));
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Result.Failure<LoginResponse>(BuildAccountLockedError(user));
            }

            return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var issued = await _tokenService.GenerateAccessToken(user, cancellationToken);

        var refreshEntity = RefreshToken.Create(
            user.Id,
            issued.HashedRefreshToken,
            ResolveClientIp(),
            issued.RefreshTokenExpiresAt,
            deviceId: null,
            userAgent: ResolveUserAgent());

        await _dbContext.AddRefreshTokenAsync(refreshEntity, cancellationToken);

        // 5.4.3 — successful login only (no failed-login noise / user enumeration)
        await _activityLogger.LogAndSaveAsync(
            userId: user.Id,
            entityType: ActivityLogConstants.EntityTypes.Auth,
            action: ActivityLogConstants.Actions.AuthLogin,
            description: "User signed in.",
            entityId: user.Id,
            cancellationToken: cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var setupComplete = await _dbContext.SystemSettings
            .AsNoTracking()
            .Select(s => (bool?)s.IsSetupComplete)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            AccessToken: issued.AccessToken,
            RefreshToken: issued.RefreshToken,
            Expiration: issued.Expiration,
            User: new LoginUserSummary(
                Id: user.Id.ToString(),
                Email: user.Email ?? email,
                FullName: user.FullName,
                Locale: user.Locale,
                Roles: roles.ToArray(),
                IsSetupComplete: setupComplete)));
    }

    private static Error BuildAccountLockedError(ApplicationUser user)
    {
        var minutes = 15;
        if (user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow)
        {
            minutes = Math.Max(1, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalMinutes));
        }

        var template = DomainErrors.Auth.AccountLocked;
        return Error.Locked(
            template.Code,
            template.Title,
            template.Description.Replace("{minutes}", minutes.ToString(), StringComparison.Ordinal));
    }

    private string ResolveClientIp()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var forwarded = httpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        return httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string? ResolveUserAgent() =>
        _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();
}
