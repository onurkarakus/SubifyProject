using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Features.Auth.Refresh;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>
/// Task 3.1.3 — rotation + reuse/theft detection against real EF + Identity (SQLite).
/// </summary>
public class RefreshTokenRotationTests
{
    [Fact]
    public async Task Refresh_rotates_token_and_revokes_old_as_replaced()
    {
        await using var harness = await AuthTestHarness.CreateAsync();
        var user = await harness.CreateUserAsync("rotate@test.local");
        var plain1 = await harness.IssueRefreshAsync(user);

        var result = await harness.SendRefreshAsync(plain1);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.NotEqual(plain1, result.Value.RefreshToken);

        var tokens = (await harness.Db.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync())
            .OrderBy(t => t.CreatedAt)
            .ToList();

        Assert.Equal(2, tokens.Count);
        var oldToken = tokens.Single(t => t.IsRevoked);
        var newToken = tokens.Single(t => !t.IsRevoked);
        Assert.Equal(RefreshToken.ReasonReplaced, oldToken.ReasonRevoked);
        Assert.Equal(newToken.TokenHash, oldToken.ReplacedByTokenHash);
        Assert.True(newToken.IsActive());
    }

    [Fact]
    public async Task Refresh_with_already_rotated_token_detects_theft_and_revokes_all()
    {
        await using var harness = await AuthTestHarness.CreateAsync();
        var user = await harness.CreateUserAsync("theft@test.local");
        var plain1 = await harness.IssueRefreshAsync(user);

        var first = await harness.SendRefreshAsync(plain1);
        Assert.True(first.IsSuccess);
        var plain2 = first.Value.RefreshToken;

        // Attacker (or lagging client) reuses plain1
        var reuse = await harness.SendRefreshAsync(plain1);

        Assert.True(reuse.IsFailure);
        Assert.Equal(DomainErrors.Auth.RefreshTokenReuseDetected.Code, reuse.Error.Code);

        var tokens = await harness.Db.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        Assert.All(tokens, t => Assert.True(t.IsRevoked));
        Assert.Contains(tokens, t => t.ReasonRevoked == RefreshToken.ReasonTheftDetected);

        // Valid rotated token plain2 must also be dead after bulk revoke
        var afterTheft = await harness.SendRefreshAsync(plain2);
        Assert.True(afterTheft.IsFailure);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_fails_invalid()
    {
        await using var harness = await AuthTestHarness.CreateAsync();
        var result = await harness.SendRefreshAsync(RefreshTokenHasher.GeneratePlainText());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Auth.InvalidRefreshToken.Code, result.Error.Code);
    }

    private sealed class AuthTestHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private AuthTestHarness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public SubifyDbContext Db =>
            _provider.GetRequiredService<SubifyDbContext>();

        public static async Task<AuthTestHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>(o =>
                {
                    o.Password.RequireDigit = false;
                    o.Password.RequireLowercase = false;
                    o.Password.RequireUppercase = false;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 6;
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

            services.AddScoped<Application.Common.Interfaces.ITokenService, TokenService>();
            services.AddScoped<Application.Common.Interfaces.ISubifyDbContext>(sp =>
                sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<RefreshHandler>();

            var provider = services.BuildServiceProvider();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                if (!await roleManager.RoleExistsAsync(AppRoles.User))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.User)
                    {
                        Id = Guid.CreateVersion7()
                    });
                }
            }

            // Minimal HttpContext for IP resolution
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext();

            return new AuthTestHarness(connection, provider);
        }

        public async Task<ApplicationUser> CreateUserAsync(string email)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser();
            user.ApplyRegistrationProfile("Test User", email);
            var result = await users.CreateAsync(user, "Password1");
            Assert.True(result.Succeeded, string.Join(",", result.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, AppRoles.User);
            return user;
        }

        public async Task<string> IssueRefreshAsync(ApplicationUser user)
        {
            await using var scope = _provider.CreateAsyncScope();
            var tokens = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.ITokenService>();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var fresh = await users.FindByIdAsync(user.Id.ToString());
            Assert.NotNull(fresh);

            var issued = await tokens.GenerateAccessToken(fresh!);
            db.RefreshTokens.Add(RefreshToken.Create(
                fresh!.Id,
                issued.HashedRefreshToken,
                "127.0.0.1",
                issued.RefreshTokenExpiresAt));
            await db.SaveChangesAsync();
            return issued.RefreshToken;
        }

        public async Task<Domain.Shared.Result<RefreshResponse>> SendRefreshAsync(string plainRefresh)
        {
            await using var scope = _provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<RefreshHandler>();
            return await handler.Handle(new RefreshCommand(plainRefresh), CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
