namespace Subify.Domain.Constants;

/// <summary>
/// Canonical Identity security policy for Subify OS (task 3.4).
/// Applied in Infrastructure DI and mirrored by FluentValidation rules.
/// </summary>
public static class IdentitySecurityDefaults
{
    // --- Password (3.4.1) ---
    public const int PasswordMinLength = 8;
    public const bool PasswordRequireDigit = true;
    public const bool PasswordRequireLowercase = true;
    public const bool PasswordRequireUppercase = true;
    /// <summary>Special characters not required (family-friendly self-host passwords).</summary>
    public const bool PasswordRequireNonAlphanumeric = false;

    public const string PasswordDigitPattern = "[0-9]";
    public const string PasswordLowerPattern = "[a-z]";
    public const string PasswordUpperPattern = "[A-Z]";

    // --- Lockout (3.4.2) ---
    public const int LockoutMaxFailedAccessAttempts = 5;
    public const int LockoutMinutes = 15;
    public const bool LockoutAllowedForNewUsers = true;

    // --- User (3.4.3) ---
    public const bool RequireUniqueEmail = true;
    public const bool RequireConfirmedEmail = false;
    public const bool RequireConfirmedAccount = false;
}
