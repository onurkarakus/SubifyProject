using Microsoft.AspNetCore.Identity;
using Subify.Domain.Constants;

namespace Subify.Infrastructure.Identity;

/// <summary>
/// Maps <see cref="IdentitySecurityDefaults"/> onto ASP.NET Identity options (task 3.4).
/// </summary>
public static class IdentityOptionsConfiguration
{
    public static void Apply(IdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Password.RequiredLength = IdentitySecurityDefaults.PasswordMinLength;
        options.Password.RequireDigit = IdentitySecurityDefaults.PasswordRequireDigit;
        options.Password.RequireLowercase = IdentitySecurityDefaults.PasswordRequireLowercase;
        options.Password.RequireUppercase = IdentitySecurityDefaults.PasswordRequireUppercase;
        options.Password.RequireNonAlphanumeric = IdentitySecurityDefaults.PasswordRequireNonAlphanumeric;

        options.User.RequireUniqueEmail = IdentitySecurityDefaults.RequireUniqueEmail;

        options.Lockout.AllowedForNewUsers = IdentitySecurityDefaults.LockoutAllowedForNewUsers;
        options.Lockout.MaxFailedAccessAttempts = IdentitySecurityDefaults.LockoutMaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(IdentitySecurityDefaults.LockoutMinutes);

        options.SignIn.RequireConfirmedEmail = IdentitySecurityDefaults.RequireConfirmedEmail;
        options.SignIn.RequireConfirmedAccount = IdentitySecurityDefaults.RequireConfirmedAccount;
    }
}
