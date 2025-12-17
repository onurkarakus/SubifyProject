namespace Subify.Domain.Models.RequestEntities.Auth;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
