namespace Subify.Application.Features.Auth.Login;

/// <summary>Successful login with user summary (tasks 3.2.2 / 3.2.10).</summary>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration,
    LoginUserSummary User);

public sealed record LoginUserSummary(
    string Id,
    string Email,
    string FullName,
    string Locale,
    IReadOnlyList<string> Roles,
    bool? IsSetupComplete);
