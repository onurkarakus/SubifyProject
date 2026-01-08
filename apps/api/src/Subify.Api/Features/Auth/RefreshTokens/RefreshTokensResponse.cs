namespace Subify.Api.Features.Auth.RefreshTokens;

public record RefreshTokensResponse(string Email, string AccessToken, string RefreshToken, DateTime Expiration);
