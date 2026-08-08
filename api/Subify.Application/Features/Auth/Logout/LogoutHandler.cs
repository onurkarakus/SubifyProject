using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Logout;

/// <summary>
/// Logout (task 3.2.4): revoke refresh token(s) with reason <c>logout</c>.
/// Idempotent — unknown/already-revoked token still returns success (no session leak).
/// Logs successful logout as activity (5.4.3).
/// </summary>
public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IActivityLogger _activityLogger;

    public LogoutHandler(
        ISubifyDbContext db,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IActivityLogger activityLogger)
    {
        _db = db;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var ip = ResolveClientIp();

        if (request.AllSessions)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
            }

            var userId = _currentUser.UserId.Value;
            var active = await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in active)
            {
                token.Revoke(RefreshToken.ReasonLogout, ip);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await WriteLogoutActivityAsync(userId, allSessions: true, cancellationToken);
            return Result.Success();
        }

        // Single refresh token logout (works without access token)
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure(DomainErrors.Auth.InvalidRefreshToken);
        }

        string hash;
        try
        {
            hash = _tokenService.HashRefreshToken(request.RefreshToken);
        }
        catch (ArgumentException)
        {
            return Result.Success(); // treat as already logged out
        }

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is not null && !existing.IsRevoked)
        {
            existing.Revoke(RefreshToken.ReasonLogout, ip);
            await _db.SaveChangesAsync(cancellationToken);
            await WriteLogoutActivityAsync(existing.UserId, allSessions: false, cancellationToken);
        }

        return Result.Success();
    }

    private Task WriteLogoutActivityAsync(Guid userId, bool allSessions, CancellationToken cancellationToken) =>
        _activityLogger.LogAndSaveAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.Auth,
            action: ActivityLogConstants.Actions.AuthLogout,
            description: allSessions ? "User signed out of all sessions." : "User signed out.",
            entityId: userId,
            cancellationToken: cancellationToken);

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
}
