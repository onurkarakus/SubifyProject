using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Profile;
using Subify.Application.Features.Profile.UpdateNotificationSettings;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.3.5 / 15.x — notification prefs (days + optional email).</summary>
public class UpdateNotificationSettingsHandlerTests
{
    [Fact]
    public async Task Update_sets_days_and_email_enabled()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new UpdateNotificationSettingsCommand(
            PushEnabled: true,
            DaysBeforeRenewal: 7,
            EmailEnabled: true));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(7, result.Value.DaysBeforeRenewal);
        Assert.True(result.Value.PushEnabled);
        Assert.True(result.Value.EmailEnabled);

        using var scope = harness.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<SubifyDbContext>()
            .NotificationSettings.SingleAsync(n => n.UserId == userId);
        Assert.Equal(7, row.DaysBeforeRenewal);
        Assert.True(row.EmailEnabled);
        Assert.True(row.PushEnabled);
    }

    [Fact]
    public async Task Update_creates_row_if_missing()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", seedNotifications: false);
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new UpdateNotificationSettingsCommand(
            PushEnabled: false,
            DaysBeforeRenewal: 1));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(1, result.Value.DaysBeforeRenewal);

        using var scope = harness.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<SubifyDbContext>()
            .NotificationSettings.AnyAsync(n => n.UserId == userId));
    }

    [Fact]
    public async Task Update_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync(new UpdateNotificationSettingsCommand(null, 3));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private Harness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>(o =>
                {
                    o.Password.RequireDigit = false;
                    o.Password.RequireLowercase = false;
                    o.Password.RequireUppercase = false;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 6;
                    o.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SubifyDbContext>();

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<UpdateNotificationSettingsHandler>();

            var provider = services.BuildServiceProvider();
            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<SubifyDbContext>().Database.EnsureCreatedAsync();
            }

            return new Harness(connection, provider);
        }

        public void SetUser(Guid userId)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = [AppRoles.User];
        }

        public async Task<Guid> SeedUserAsync(string email, bool seedNotifications = true)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            if (seedNotifications)
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                db.NotificationSettings.Add(NotificationSetting.CreateDefaults(user.Id));
                await db.SaveChangesAsync();
            }

            return user.Id;
        }

        public async Task<Result<NotificationSettingsResponse>> HandleAsync(
            UpdateNotificationSettingsCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateNotificationSettingsHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public bool IsAuthenticated { get; set; }
            public Guid? UserId { get; set; }
            public string? Email { get; set; }
            public string? Locale { get; set; }
            public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
            public bool IsInRole(string role) =>
                Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
            public Guid GetRequiredUserId() => UserId ?? throw new UnauthorizedAccessException();
        }
    }
}
