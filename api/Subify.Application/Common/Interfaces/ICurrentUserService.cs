namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Access to the authenticated user from the current HTTP request (JWT claims).
/// Handlers should depend on this instead of parsing <c>ClaimsPrincipal</c> directly.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>True when the request has an authenticated principal.</summary>
    bool IsAuthenticated { get; }

    /// <summary>User id from JWT <c>sub</c> / nameidentifier; null if anonymous or invalid.</summary>
    Guid? UserId { get; }

    /// <summary>Email claim; null if missing.</summary>
    string? Email { get; }

    /// <summary>Locale claim when present (e.g. profile locale).</summary>
    string? Locale { get; }

    /// <summary>Role claims for the current user.</summary>
    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);

    /// <summary>
    /// Returns <see cref="UserId"/> or throws if the caller is not authenticated.
    /// Prefer in authorized handlers after auth middleware has run.
    /// </summary>
    Guid GetRequiredUserId();
}
