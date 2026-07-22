using Microsoft.AspNetCore.Authorization;
using Subify.Domain.Constants;

namespace Subify.Infrastructure.Authorization;

/// <summary>ASP.NET authorization policy names (tasks 3.3.3 / 3.3.4).</summary>
public static class AuthPolicies
{
    public const string SuperAdmin = "RequireSuperAdmin";
    public const string AdminOrAbove = "RequireAdminOrAbove";
    public const string Authenticated = "RequireAuthenticatedUser";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdmin, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AppRoles.SuperAdmin));

        options.AddPolicy(AdminOrAbove, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AppRoles.SuperAdmin, AppRoles.Admin));

        options.AddPolicy(Authenticated, policy =>
            policy.RequireAuthenticatedUser());

        // Task 3.3.4 — default deny: endpoints must AllowAnonymous or RequireAuthorization
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
}
