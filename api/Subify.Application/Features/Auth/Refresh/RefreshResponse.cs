namespace Subify.Application.Features.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration);
