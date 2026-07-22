using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Persistence;
using Subify.Infrastructure.Persistence.Seeding;

namespace Subify.Api.Tests;

/// <summary>
/// Task 2.3.10 — second seed run must not create duplicates.
/// </summary>
public class SeedIdempotencyTests
{
    [Fact]
    public async Task Full_seed_pipeline_is_idempotent_on_second_run()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<SubifyDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SubifyDbContext>();
        services.AddDataSeeders();

        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        // First run — insert catalogs
        await DatabaseSeeder.SeedAsync(provider);
        var afterFirst = await SnapshotAsync(provider);

        // Second run — must be a no-op for counts
        await DatabaseSeeder.SeedAsync(provider);
        var afterSecond = await SnapshotAsync(provider);

        Assert.Equal(afterFirst, afterSecond);

        // Expected catalog sizes
        Assert.Equal(AppRoles.All.Count, afterSecond.Roles);
        Assert.Equal(SystemCategories.All.Count, afterSecond.Categories);
        Assert.Equal(SystemProviders.All.Count, afterSecond.Providers);
        Assert.Equal(SystemResources.All.Count, afterSecond.Resources);
        Assert.Equal(1, afterSecond.SystemSettings);
        Assert.Equal(SystemEmailTemplates.All.Count, afterSecond.EmailTemplates);
    }

    [Fact]
    public async Task Categories_seeder_does_not_duplicate_after_soft_delete_filter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<SubifyDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SubifyDbContext>();
        services.AddScoped<CategoriesDataSeeder>();

        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            await db.Database.EnsureCreatedAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<CategoriesDataSeeder>();
            await seeder.SeedAsync();
            await seeder.SeedAsync();

            var active = await db.Categories.CountAsync();
            var all = await db.Categories.IgnoreQueryFilters().CountAsync();

            Assert.Equal(SystemCategories.All.Count, active);
            Assert.Equal(SystemCategories.All.Count, all);
        }
    }

    private static async Task<SeedSnapshot> SnapshotAsync(IServiceProvider root)
    {
        await using var scope = root.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        return new SeedSnapshot(
            Roles: roleManager.Roles.Count(),
            Categories: await db.Categories.IgnoreQueryFilters().CountAsync(),
            Providers: await db.Providers.IgnoreQueryFilters().CountAsync(),
            Resources: await db.Resources.CountAsync(),
            SystemSettings: await db.SystemSettings.CountAsync(),
            EmailTemplates: await db.EmailTemplates.CountAsync());
    }

    private sealed record SeedSnapshot(
        int Roles,
        int Categories,
        int Providers,
        int Resources,
        int SystemSettings,
        int EmailTemplates);
}
