using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Identity;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 3.4 — Identity options mapping + unique email enforcement.</summary>
public class IdentityOptionsConfigurationTests
{
    [Fact]
    public void Apply_maps_all_IdentitySecurityDefaults()
    {
        var options = new IdentityOptions();
        IdentityOptionsConfiguration.Apply(options);

        Assert.Equal(IdentitySecurityDefaults.PasswordMinLength, options.Password.RequiredLength);
        Assert.Equal(IdentitySecurityDefaults.PasswordRequireDigit, options.Password.RequireDigit);
        Assert.Equal(IdentitySecurityDefaults.PasswordRequireLowercase, options.Password.RequireLowercase);
        Assert.Equal(IdentitySecurityDefaults.PasswordRequireUppercase, options.Password.RequireUppercase);
        Assert.Equal(IdentitySecurityDefaults.PasswordRequireNonAlphanumeric, options.Password.RequireNonAlphanumeric);

        Assert.Equal(IdentitySecurityDefaults.RequireUniqueEmail, options.User.RequireUniqueEmail);

        Assert.Equal(IdentitySecurityDefaults.LockoutMaxFailedAccessAttempts, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(IdentitySecurityDefaults.LockoutMinutes), options.Lockout.DefaultLockoutTimeSpan);
        Assert.Equal(IdentitySecurityDefaults.LockoutAllowedForNewUsers, options.Lockout.AllowedForNewUsers);

        Assert.Equal(IdentitySecurityDefaults.RequireConfirmedEmail, options.SignIn.RequireConfirmedEmail);
        Assert.Equal(IdentitySecurityDefaults.RequireConfirmedAccount, options.SignIn.RequireConfirmedAccount);
    }

    [Fact]
    public async Task Identity_rejects_duplicate_email_when_RequireUniqueEmail()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(IdentityOptionsConfiguration.Apply)
            .AddEntityFrameworkStores<SubifyDbContext>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        await db.Database.EnsureCreatedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var a = new ApplicationUser { Id = Guid.CreateVersion7() };
        a.ApplyRegistrationProfile("A", "same@subify.local");
        var first = await users.CreateAsync(a, "Password1");
        Assert.True(first.Succeeded, string.Join(",", first.Errors.Select(e => e.Description)));

        var b = new ApplicationUser { Id = Guid.CreateVersion7() };
        b.ApplyRegistrationProfile("B", "same@subify.local");
        var second = await users.CreateAsync(b, "Password1");

        Assert.False(second.Succeeded);
        Assert.Contains(second.Errors, e =>
            e.Code is "DuplicateUserName" or "DuplicateEmail"
                or "DuplicateUserNameNormalized" or "DuplicateEmailNormalized");
    }

    [Fact]
    public async Task Identity_rejects_password_below_policy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(IdentityOptionsConfiguration.Apply)
            .AddEntityFrameworkStores<SubifyDbContext>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        await db.Database.EnsureCreatedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.CreateVersion7() };
        user.ApplyRegistrationProfile("Weak", "weak@subify.local");

        var result = await users.CreateAsync(user, "short");
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code.StartsWith("Password", StringComparison.Ordinal));
    }
}
