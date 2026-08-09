using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Profile;
using Subify.Application.Features.Profile.GetProfile;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.3.1 — GET profile.</summary>
public class GetProfileHandlerTests
{
    [Fact]
    public async Task Get_returns_preferences_email_and_roles()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync(
            "ada@subify.local",
            "Ada Lovelace",
            AppRoles.User,
            locale: "en",
            currency: "USD",
            budget: 250m,
            theme: ThemeColors.OceanBlue,
            dark: true);

        harness.SetUser(userId, [AppRoles.User]);

        var result = await harness.HandleAsync();
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("ada@subify.local", result.Value.Email);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal("USD", result.Value.MainCurrency);
        Assert.Equal(250m, result.Value.MonthlyBudget);
        Assert.Equal(ThemeColors.OceanBlue, result.Value.ApplicationThemeColor);
        Assert.True(result.Value.DarkTheme);
        Assert.Contains(AppRoles.User, result.Value.Roles);
    }

    [Fact]
    public async Task Get_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync();
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
            services.AddScoped<GetProfileHandler>();

            var provider = services.BuildServiceProvider();

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

        public void SetUser(Guid userId, IReadOnlyList<string> roles)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = roles;
        }

        public async Task<Guid> SeedUserAsync(
            string email,
            string fullName,
            string role,
            string locale,
            string currency,
            decimal? budget,
            string theme,
            bool dark)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(fullName, email);
            user.UpdateProfile(
                fullName: fullName,
                locale: locale,
                mainCurrency: currency,
                monthlyBudget: budget,
                applicationThemeColor: theme,
                darkTheme: dark);
            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result<ProfileResponse>> HandleAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetProfileHandler>()
                .Handle(new GetProfileQuery(), CancellationToken.None);
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
