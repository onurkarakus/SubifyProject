namespace Subify.Domain.Models.Auth;

public record GenerateTokenResponse(
    string AccessToken,
    string RefreshToken,
    string HashedRefreshToken,
    DateTime Expiration);
