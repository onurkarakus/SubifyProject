using Subify.Domain.Constants;

namespace Subify.Domain.Tests;

/// <summary>Task 3.4 — documented OS identity defaults.</summary>
public class IdentitySecurityDefaultsTests
{
    [Fact]
    public void Password_policy_matches_documented_os_rules()
    {
        Assert.Equal(8, IdentitySecurityDefaults.PasswordMinLength);
        Assert.True(IdentitySecurityDefaults.PasswordRequireDigit);
        Assert.True(IdentitySecurityDefaults.PasswordRequireLowercase);
        Assert.True(IdentitySecurityDefaults.PasswordRequireUppercase);
        Assert.False(IdentitySecurityDefaults.PasswordRequireNonAlphanumeric);
    }

    [Fact]
    public void Lockout_policy_matches_login_handler_expectations()
    {
        Assert.Equal(5, IdentitySecurityDefaults.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, IdentitySecurityDefaults.LockoutMinutes);
        Assert.True(IdentitySecurityDefaults.LockoutAllowedForNewUsers);
    }

    [Fact]
    public void Unique_email_and_no_confirm_are_enforced_in_defaults()
    {
        Assert.True(IdentitySecurityDefaults.RequireUniqueEmail);
        Assert.False(IdentitySecurityDefaults.RequireConfirmedEmail);
        Assert.False(IdentitySecurityDefaults.RequireConfirmedAccount);
    }
}
