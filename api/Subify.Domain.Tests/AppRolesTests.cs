using Subify.Domain.Constants;

namespace Subify.Domain.Tests;

public class AppRolesTests
{
    [Fact]
    public void All_contains_exactly_SuperAdmin_Admin_User()
    {
        Assert.Equal(
            new[] { AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.User },
            AppRoles.All);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("superadmin")]
    [InlineData("Admin")]
    [InlineData("User")]
    public void IsDefined_true_for_known_roles(string role)
    {
        Assert.True(AppRoles.IsDefined(role));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Moderator")]
    public void IsDefined_false_for_unknown_roles(string? role)
    {
        Assert.False(AppRoles.IsDefined(role));
    }
}
