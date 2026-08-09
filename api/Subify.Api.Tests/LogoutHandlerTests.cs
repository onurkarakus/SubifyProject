using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Auth.Login;
using Subify.Application.Features.Auth.Logout;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.4.3 — logout activity logging.</summary>
public class LogoutHandlerTests
{
    [Fact]
    public async Task Logout_single_token_writes_auth_logout_activity()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.CreateUserAsync("out@subify.local", "Password1");

        var login = await harness.LoginAsync("out@subify.local", "Password1");
        Assert.True(login.IsSuccess);

        var logout = await harness.LogoutAsync(new LogoutCommand(
            RefreshToken: login.Value.RefreshToken,
            AllSessions: false));
        Assert.True(logout.IsSuccess);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        // SQLite cannot ORDER BY DateTimeOffset in SQL — materialize then sort in memory.
        var logs = (await db.ActivityLogs.ToListAsync())
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToList();
        Assert.Equal(2, logs.Count);
        Assert.Equal(ActivityLogConstants.Actions.AuthLogin, logs[0].Action);
        Assert.Equal(ActivityLogConstants.Actions.AuthLogout, logs[1].Action);
        Assert.Equal(ActivityLogConstants.EntityTypes.Auth, logs[1].EntityType);
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
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<LoginHandler>();
            services.AddScoped<LogoutHandler>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                if (!await roles.RoleExistsAsync(AppRoles.User))
                {
                    await roles.CreateAsync(new IdentityRole<Guid>(AppRoles.User) { Id = Guid.CreateVersion7() });
                }
            }

            return new Harness(connection, provider);
        }

        public async Task CreateUserAsync(string email, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile("Logout Test", email);
            user.EmailConfirmed = true;
            await users.CreateAsync(user, password);
            await users.AddToRoleAsync(user, AppRoles.User);
        }

        public async Task<Domain.Shared.Result<LoginResponse>> LoginAsync(string email, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<LoginHandler>()
                .Handle(new LoginCommand(email, password), CancellationToken.None);
        }

        public async Task<Domain.Shared.Result> LogoutAsync(LogoutCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<LogoutHandler>()
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
            public bool IsInRole(string role) => false;
            public Guid GetRequiredUserId() => UserId ?? throw new UnauthorizedAccessException();
        }
    }
}
