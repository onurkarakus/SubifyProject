using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.4.1 — central IActivityLogger.</summary>
public class ActivityLoggerTests
{
    [Fact]
    public async Task LogAsync_stages_row_with_http_context_metadata()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
        services.AddScoped<IActivityLogger, ActivityLogger>();

        await using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.10");
        http.Request.Headers.UserAgent = "SubifyTests/1.0";
        accessor.HttpContext = http;

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ActivityLog has FK to user — seed minimal identity user via raw entity is heavy;
        // use EnsureCreated + disable FK for test: create ApplicationUser via DbContext if set.
        var userId = Guid.CreateVersion7();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "a@test.local",
            NormalizedUserName = "A@TEST.LOCAL",
            Email = "a@test.local",
            NormalizedEmail = "A@TEST.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FullName = "A",
            Locale = "tr",
            MainCurrency = "TRY",
            ApplicationThemeColor = ThemeColors.Default
        });
        await db.SaveChangesAsync();

        var logger = scope.ServiceProvider.GetRequiredService<IActivityLogger>();
        await logger.LogAndSaveAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.Profile,
            action: ActivityLogConstants.Actions.ProfileUpdated,
            description: "test",
            entityId: userId,
            oldValues: "{\"a\":1}",
            newValues: "{\"a\":2}");

        var row = await db.ActivityLogs.SingleAsync();
        Assert.Equal(userId, row.UserId);
        Assert.Equal(ActivityLogConstants.Actions.ProfileUpdated, row.Action);
        Assert.Equal("203.0.113.10", row.IpAddress);
        Assert.Equal("SubifyTests/1.0", row.UserAgent);
        Assert.Equal("{\"a\":1}", row.OldValues);
        Assert.Equal("{\"a\":2}", row.NewValues);
    }
}
