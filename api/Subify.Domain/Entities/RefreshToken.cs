using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Persisted refresh token. Only <see cref="TokenHash"/> (SHA-256 hex) is stored —
/// never the plain token (task 3.1.2). Supports rotation and revoke reasons.
/// </summary>
public class RefreshToken : BaseEntity
{
    public const string ReasonLogout = "logout";
    public const string ReasonReplaced = "replaced";
    public const string ReasonTheftDetected = "theft_detected";
    public const string ReasonExpired = "expired";
    public const string ReasonAdmin = "admin";

    /// <summary>SHA-256 hex length (64). Column allows up to 128 for future algorithms.</summary>
    public const int TokenHashMaxLength = 128;

    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hex of the plain refresh token. Lookup key for rotation/logout.</summary>
    public string TokenHash { get; private set; } = null!;
    public string CreatedByIp { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReasonRevoked { get; private set; }

    /// <summary>Hash of the token that replaced this one (rotation chain).</summary>
    public string? ReplacedByTokenHash { get; private set; }

    public string? DeviceId { get; private set; }
    public string? UserAgent { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    public bool IsExpired(DateTimeOffset? utcNow = null) =>
        (utcNow ?? DateTimeOffset.UtcNow) >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>Active = not revoked and not expired.</summary>
    public bool IsActive(DateTimeOffset? utcNow = null) =>
        !IsRevoked && !IsExpired(utcNow);

    protected RefreshToken()
    {
    }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        string createdByIp,
        DateTimeOffset expiresAt,
        string? deviceId = null,
        string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        var normalizedHash = tokenHash.Trim();
        if (normalizedHash.Length > TokenHashMaxLength)
        {
            throw new ArgumentException(
                $"Token hash exceeds max length {TokenHashMaxLength}.",
                nameof(tokenHash));
        }

        return new RefreshToken
        {
            Id = GuidGenerator.NewId(),
            UserId = userId,
            TokenHash = normalizedHash,
            CreatedByIp = string.IsNullOrWhiteSpace(createdByIp) ? "Unknown" : createdByIp.Trim(),
            ExpiresAt = expiresAt,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Revoke(
        string reason,
        string? revokedByIp = null,
        string? replacedByTokenHash = null,
        DateTimeOffset? revokedAt = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = revokedAt ?? DateTimeOffset.UtcNow;
        ReasonRevoked = string.IsNullOrWhiteSpace(reason) ? ReasonLogout : reason.Trim();
        RevokedByIp = string.IsNullOrWhiteSpace(revokedByIp) ? null : revokedByIp.Trim();
        ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacedByTokenHash)
            ? null
            : replacedByTokenHash.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark as replaced during refresh-token rotation.</summary>
    public void MarkReplaced(string newTokenHash, string? revokedByIp = null) =>
        Revoke(ReasonReplaced, revokedByIp, newTokenHash);

    public void MarkTheftDetected(string? revokedByIp = null) =>
        Revoke(ReasonTheftDetected, revokedByIp);

    /// <summary>
    /// Force-mark theft even if already revoked (reuse detection on a rotated token).
    /// Updates reason/IP without clearing replacement chain.
    /// </summary>
    public void FlagReuseAsTheft(string? revokedByIp = null)
    {
        if (!IsRevoked)
        {
            MarkTheftDetected(revokedByIp);
            return;
        }

        ReasonRevoked = ReasonTheftDetected;
        if (!string.IsNullOrWhiteSpace(revokedByIp))
        {
            RevokedByIp = revokedByIp.Trim();
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

