using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Users;

/// <summary>Shared admin-user authorization helpers (7.1).</summary>
internal static class AdminUserAccess
{
    public static bool IsAdminOrAbove(ICurrentUserService current) =>
        current.IsAuthenticated
        && (current.IsInRole(AppRoles.SuperAdmin) || current.IsInRole(AppRoles.Admin));

    public static bool IsSuperAdmin(ICurrentUserService current) =>
        current.IsAuthenticated && current.IsInRole(AppRoles.SuperAdmin);

    public static Result RequireAdminOrAbove(ICurrentUserService current) =>
        IsAdminOrAbove(current)
            ? Result.Success()
            : Result.Failure(DomainErrors.UserErrors.UnAuthorized);

    public static Result RequireSuperAdmin(ICurrentUserService current) =>
        IsSuperAdmin(current)
            ? Result.Success()
            : Result.Failure(DomainErrors.UserErrors.UnAuthorized);

    /// <summary>Roles assignable via admin create/patch: User or Admin only.</summary>
    public static bool IsAssignableRole(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && (string.Equals(role.Trim(), AppRoles.User, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role.Trim(), AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

    public static string NormalizeAssignableRole(string role) =>
        string.Equals(role.Trim(), AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? AppRoles.Admin
            : AppRoles.User;
}
