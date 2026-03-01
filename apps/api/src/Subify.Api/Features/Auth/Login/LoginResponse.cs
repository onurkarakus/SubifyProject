namespace Subify.Api.Features.Auth.Login;

public record LoginResponse(string Email, string AccessToken, string RefreshToken, DateTime Expiration);
