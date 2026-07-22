using Subify.Domain.Constants;

namespace Subify.Infrastructure.Authorization;

/// <summary>ASP.NET authorization policy names (tasks 3.2.15 / 3.3.3).</summary>
public static class AuthPolicies
{
    public const string SuperAdmin = "RequireSuperAdmin";
    public const string AdminOrAbove = "RequireAdminOrAbove";
    public const string Authenticated = "RequireAuthenticatedUser";

    public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdmin, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AppRoles.SuperAdmin));

        options.AddPolicy(AdminOrAbove, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AppRoles.SuperAdmin, AppRoles.Admin));

        options.AddPolicy(Authenticated, policy =>
            policy.RequireAuthenticatedUser());
    }
}
