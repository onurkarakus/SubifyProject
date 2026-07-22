using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Auth.Login;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 3.2.2 — login credentials, lockout, no email-confirm gate.</summary>
public class LoginHandlerTests
{
    [Fact]
    public async Task Login_success_returns_tokens_and_persists_refresh_hash_only()
    {
        await using var harness = await LoginHarness.CreateAsync();
        await harness.CreateUserAsync("login@subify.local", "Password1", emailConfirmed: true);

        var result = await harness.HandleAsync(new LoginCommand("login@subify.local", "Password1"));

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Equal("login@subify.local", result.Value.User.Email);
        Assert.Equal("Login Test", result.Value.User.FullName);
        Assert.Contains(AppRoles.User, result.Value.User.Roles);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var stored = await db.RefreshTokens.SingleAsync();
        Assert.NotEqual(result.Value.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Login_succeeds_even_when_EmailConfirmed_is_false()
    {
        await using var harness = await LoginHarness.CreateAsync();
        await harness.CreateUserAsync("noconfirm@subify.local", "Password1", emailConfirmed: false);

        var result = await harness.HandleAsync(new LoginCommand("noconfirm@subify.local", "Password1"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
    }

    [Fact]
    public async Task Login_unknown_user_returns_invalid_credentials_not_not_found()
    {
        await using var harness = await LoginHarness.CreateAsync();

        var result = await harness.HandleAsync(new LoginCommand("missing@subify.local", "Password1"));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Auth.InvalidCredentials.Code, result.Error.Code);
        Assert.NotEqual(DomainErrors.UserErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Login_wrong_password_returns_invalid_credentials()
    {
        await using var harness = await LoginHarness.CreateAsync();
        await harness.CreateUserAsync("badpw@subify.local", "Password1", emailConfirmed: true);

        var result = await harness.HandleAsync(new LoginCommand("badpw@subify.local", "WrongPass1"));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Auth.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Login_locks_out_after_max_failed_attempts()
    {
        await using var harness = await LoginHarness.CreateAsync();
        await harness.CreateUserAsync("lock@subify.local", "Password1", emailConfirmed: true);

        for (var i = 0; i < 5; i++)
        {
            var fail = await harness.HandleAsync(new LoginCommand("lock@subify.local", "WrongPass1"));
            Assert.True(fail.IsFailure);
        }

        var locked = await harness.HandleAsync(new LoginCommand("lock@subify.local", "Password1"));
        Assert.True(locked.IsFailure);
        Assert.Equal(DomainErrors.Auth.AccountLocked.Code, locked.Error.Code);
        Assert.Equal(Domain.Shared.ErrorType.Locked, locked.Error.Type);
    }

    private sealed class LoginHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private LoginHarness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public static async Task<LoginHarness> CreateAsync()
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
                    o.SignIn.RequireConfirmedEmail = false;
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
                o.ClockSkewSeconds = 30;
            });

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<LoginHandler>();

            var provider = services.BuildServiceProvider();

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

            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext();

            return new LoginHarness(connection, provider);
        }

        public async Task CreateUserAsync(string email, string password, bool emailConfirmed)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile("Login Test", email);
            user.EmailConfirmed = emailConfirmed;
            user.LockoutEnabled = true;

            var create = await users.CreateAsync(user, password);
            Assert.True(create.Succeeded, string.Join(",", create.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, AppRoles.User);
        }

        public async Task<Domain.Shared.Result<LoginResponse>> HandleAsync(LoginCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<LoginHandler>();
            return await handler.Handle(command, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
