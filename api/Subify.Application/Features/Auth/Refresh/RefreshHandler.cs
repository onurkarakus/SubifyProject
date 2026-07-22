using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Refresh;

/// <summary>
/// Rotates refresh tokens (task 3.1.3):
/// active token → revoke as <c>replaced</c> + issue new pair;
/// revoked/reused token → <c>theft_detected</c> and revoke all user sessions.
/// </summary>
public sealed class RefreshHandler : IRequestHandler<RefreshCommand, Result<RefreshResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshHandler(
        ISubifyDbContext db,
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _tokenService = tokenService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<RefreshResponse>> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        var ip = ResolveClientIp();
        var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();

        string tokenHash;
        try
        {
            tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        }
        catch (ArgumentException)
        {
            return Result.Failure<RefreshResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null)
        {
            return Result.Failure<RefreshResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        // --- Reuse / theft detection (rotated or otherwise revoked token presented again) ---
        if (existing.IsRevoked)
        {
            await HandleReuseDetectedAsync(existing, ip, cancellationToken);
            return Result.Failure<RefreshResponse>(DomainErrors.Auth.RefreshTokenReuseDetected);
        }

        // --- Expired ---
        if (existing.IsExpired())
        {
            existing.Revoke(RefreshToken.ReasonExpired, ip);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Failure<RefreshResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
        {
            existing.Revoke(RefreshToken.ReasonAdmin, ip);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Failure<RefreshResponse>(DomainErrors.Auth.InvalidRefreshToken);
        }

        // --- Rotate: new access + refresh, old marked replaced ---
        var issued = await _tokenService.GenerateAccessToken(user, cancellationToken);

        existing.MarkReplaced(issued.HashedRefreshToken, ip);

        var replacement = RefreshToken.Create(
            user.Id,
            issued.HashedRefreshToken,
            ip,
            issued.RefreshTokenExpiresAt,
            deviceId: existing.DeviceId,
            userAgent: string.IsNullOrWhiteSpace(userAgent) ? existing.UserAgent : userAgent);

        await _db.RefreshTokens.AddAsync(replacement, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshResponse(
            issued.AccessToken,
            issued.RefreshToken,
            issued.Expiration));
    }

    private async Task HandleReuseDetectedAsync(
        RefreshToken reused,
        string ip,
        CancellationToken cancellationToken)
    {
        reused.FlagReuseAsTheft(ip);

        // Revoke every still-active token for this user (session family wipe)
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == reused.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.MarkTheftDetected(ip);
        }

        await _db.SaveChangesAsync(cancellationToken);
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
}
