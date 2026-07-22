namespace Subify.Domain.Constants;

/// <summary>
/// ASP.NET Identity role names for Subify OS (task 2.3.4).
/// Seeded at startup; used by authorization policies (3.3.x).
/// </summary>
public static class AppRoles
{
    /// <summary>Instance owner — setup, settings, user admin, password reset.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Can manage users (limited); not full instance owner.</summary>
    public const string Admin = "Admin";

    /// <summary>Default family/member role after register or invite.</summary>
    public const string User = "User";

    /// <summary>All roles seeded by <c>RolesDataSeeder</c> (order: SuperAdmin → Admin → User).</summary>
    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        Admin,
        User
    ];

    public static bool IsDefined(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);
}
