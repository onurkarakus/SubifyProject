namespace Subify.Domain.Models.Auth;

/// <summary>
/// Access JWT + refresh token pair issued at login/refresh.
/// <see cref="RefreshToken"/> is plain (client only); <see cref="HashedRefreshToken"/> is for DB.
/// </summary>
public record GenerateTokenResponse(
    string AccessToken,
    string RefreshToken,
    string HashedRefreshToken,
    DateTime Expiration,
    DateTimeOffset RefreshTokenExpiresAt);
