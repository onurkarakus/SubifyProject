namespace Subify.Domain.Constants;

/// <summary>
/// JWT / principal claim type names used by Subify OS (task 3.1.1).
/// Aligns with JWT bearer <c>MapInboundClaims = false</c> and <c>ICurrentUserService</c>.
/// Pure string constants — no Identity/JWT package dependency in Domain.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>User id (GUID string). JWT registered name <c>sub</c>.</summary>
    public const string Subject = "sub";

    /// <summary>Email address. JWT registered name <c>email</c>.</summary>
    public const string Email = "email";

    /// <summary>JWT unique id (UUID v7). JWT registered name <c>jti</c>.</summary>
    public const string JwtId = "jti";

    /// <summary>User locale (tr / en). JWT registered name <c>locale</c>.</summary>
    public const string Locale = "locale";

    /// <summary>
    /// Role claim type — same as <c>ClaimTypes.Role</c> URI.
    /// Matches JWT options <c>RoleClaimType</c>.
    /// </summary>
    public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
}
