using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Auth.AdminResetPassword;
using Subify.Application.Features.Auth.Login;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Tasks 3.2.15 / 7.5.1 — SuperAdmin reset-password for admin users table.</summary>
public class AdminResetPasswordHandlerTests
{
    [Fact]
    public async Task SuperAdmin_resets_password_and_target_can_login()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        var targetId = await harness.SeedUserAsync("member@subify.local", AppRoles.User, "OldPassword1");
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var reset = await harness.ResetAsync(targetId, "NewPassword1");
        Assert.True(reset.IsSuccess, reset.IsFailure ? reset.Error.Code : null);

        var oldLogin = await harness.LoginAsync("member@subify.local", "OldPassword1");
        Assert.True(oldLogin.IsFailure);

        var newLogin = await harness.LoginAsync("member@subify.local", "NewPassword1");
        Assert.True(newLogin.IsSuccess, newLogin.IsFailure ? newLogin.Error.Code : null);
    }

    [Fact]
    public async Task Reset_revokes_refresh_sessions()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        var targetId = await harness.SeedUserAsync("member@subify.local", AppRoles.User, "Password1");

        // Create an active refresh token row
        await harness.AddRefreshTokenAsync(targetId);
        Assert.Equal(1, await harness.CountActiveSessionsAsync(targetId));

        harness.SetUser(superId, AppRoles.SuperAdmin);
        Assert.True((await harness.ResetAsync(targetId, "NewPassword1")).IsSuccess);
        Assert.Equal(0, await harness.CountActiveSessionsAsync(targetId));
    }

    [Fact]
    public async Task Reset_writes_audit_without_password()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        var targetId = await harness.SeedUserAsync("member@subify.local", AppRoles.User, "Password1");
        harness.SetUser(superId, AppRoles.SuperAdmin);

        const string secret = "SuperSecret9";
        Assert.True((await harness.ResetAsync(targetId, secret)).IsSuccess);

        var logs = await harness.GetActivityAsync(superId);
        var log = Assert.Single(logs, a => a.Action == ActivityLogConstants.Actions.AdminPasswordReset);
        Assert.Equal(ActivityLogConstants.EntityTypes.Auth, log.EntityType);
        Assert.Equal(targetId, log.EntityId);
        Assert.Contains("member@subify.local", log.NewValues ?? "");
        Assert.DoesNotContain(secret, log.NewValues ?? "");
        Assert.DoesNotContain(secret, log.Description ?? "");
    }

    [Fact]
    public async Task Cannot_reset_own_password_via_admin_endpoint()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var self = await harness.ResetAsync(superId, "NewPassword1");
        Assert.Equal(DomainErrors.UserErrors.UseChangePassword.Code, self.Error.Code);
    }

    [Fact]
    public async Task Non_super_admin_and_missing_user_fail()
    {
        await using var harness = await Harness.CreateAsync();
        var targetId = await harness.SeedUserAsync("member@subify.local", AppRoles.User, "Password1");
        var adminId = await harness.SeedUserAsync("admin@subify.local", AppRoles.Admin);

        harness.SetUser(adminId, AppRoles.Admin);
        var denied = await harness.ResetAsync(targetId, "NewPassword1");
        Assert.Equal(DomainErrors.UserErrors.AccessDenied.Code, denied.Error.Code);

        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);
        var missing = await harness.ResetAsync(Guid.CreateVersion7(), "NewPassword1");
        Assert.Equal(DomainErrors.UserErrors.NotFound.Code, missing.Error.Code);
    }

    [Fact]
    public async Task Reset_clears_temporary_lockout()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        var targetId = await harness.SeedUserAsync("locked@subify.local", AppRoles.User, "Password1");

        await harness.LockUserAsync(targetId);
        Assert.True(await harness.IsLockedOutAsync(targetId));

        harness.SetUser(superId, AppRoles.SuperAdmin);
        Assert.True((await harness.ResetAsync(targetId, "NewPassword1")).IsSuccess);
        Assert.False(await harness.IsLockedOutAsync(targetId));

        var login = await harness.LoginAsync("locked@subify.local", "NewPassword1");
        Assert.True(login.IsSuccess, login.IsFailure ? login.Error.Code : null);
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

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>(o =>
                {
                    o.Password.RequireDigit = true;
                    o.Password.RequireLowercase = true;
                    o.Password.RequireUppercase = true;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 8;
                    o.User.RequireUniqueEmail = true;
                    o.Lockout.AllowedForNewUsers = true;
                    o.Lockout.MaxFailedAccessAttempts = 5;
                    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SubifyDbContext>();

            services.Configure<JwtOptions>(o =>
            {
                o.Issuer = "SubifyOS";
                o.Audience = "SubifyOSClient";
                o.SecretKey = "SuperSecretKeyForSubifyOsProjectWhichNeedsToBeLongEnough";
                o.ExpirationInMinutes = 60;
                o.RefreshTokenExpirationDays = 7;
            });

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<AdminResetPasswordHandler>();
            services.AddScoped<LoginHandler>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                foreach (var name in AppRoles.All)
                {
                    if (!await roles.RoleExistsAsync(name))
                    {
                        await roles.CreateAsync(new IdentityRole<Guid>(name) { Id = Guid.CreateVersion7() });
                    }
                }
            }

            return new Harness(connection, provider);
        }

        public void SetUser(Guid userId, string role)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = [role];
        }

        public async Task<Guid> SeedUserAsync(string email, string role, string password = "Password1")
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result> ResetAsync(Guid userId, string newPassword)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<AdminResetPasswordHandler>()
                .Handle(new AdminResetPasswordCommand(userId, newPassword), CancellationToken.None);
        }

        public async Task<Result<LoginResponse>> LoginAsync(string email, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<LoginHandler>()
                .Handle(new LoginCommand(email, password), CancellationToken.None);
        }

        public async Task AddRefreshTokenAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var token = RefreshToken.Create(
                userId,
                "hash-" + Guid.NewGuid().ToString("N"),
                "127.0.0.1",
                DateTimeOffset.UtcNow.AddDays(7));
            db.RefreshTokens.Add(token);
            await db.SaveChangesAsync();
        }

        public async Task<int> CountActiveSessionsAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.RefreshTokens.CountAsync(t => t.UserId == userId && t.RevokedAt == null);
        }

        public async Task LockUserAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString())
                       ?? throw new InvalidOperationException("missing");
            await users.SetLockoutEnabledAsync(user, true);
            await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        }

        public async Task<bool> IsLockedOutAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString())
                       ?? throw new InvalidOperationException("missing");
            return await users.IsLockedOutAsync(user);
        }

        public async Task<List<ActivityLog>> GetActivityAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.ActivityLogs.AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync();
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
