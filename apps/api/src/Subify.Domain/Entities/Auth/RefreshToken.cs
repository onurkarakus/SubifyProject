using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;

namespace Subify.Domain.Entities.Auth;

/// <summary>
/// JWT refresh tokens for token rotation.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    public Guid? UserId { get; set; }

    /// <summary>
    /// SHA256 hash of the token (never store raw token).
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Token has been revoked (logout, rotation, security).
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When the token was revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// The token that replaced this one (for rotation tracking).
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Device/client identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// IP address at token creation.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent at token creation.
    /// </summary>
    public string? UserAgent { get; set; }

    // Navigation
    public ApplicationUser? User { get; set; } = null!;
}
