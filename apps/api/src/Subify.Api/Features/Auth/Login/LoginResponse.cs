namespace Subify.Api.Features.Auth.Login;

public record LoginResponse(string email, string accessToken, string refreshToken, DateTime expiration);
