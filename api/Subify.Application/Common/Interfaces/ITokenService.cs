using Subify.Domain.Entities;
using Subify.Domain.Models.Auth;

namespace Subify.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Issues a JWT access token (sub, email, jti, locale, roles) and a new refresh token pair.
    /// Tasks 3.1.1 / 3.1.2.
    /// </summary>
    Task<GenerateTokenResponse> GenerateAccessToken(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new opaque refresh token: plain text for the client, SHA-256 hash for storage.
    /// </summary>
    RefreshTokenMaterial CreateRefreshTokenMaterial(DateTimeOffset? utcNow = null);

    /// <summary>
    /// Hashes a client-provided plain refresh token for DB lookup (same algorithm as store).
    /// </summary>
    string HashRefreshToken(string plainRefreshToken);
}
