using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Profile;
using Subify.Application.Features.Profile.UpdateProfile;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.3.2 / 5.3.3 / 5.3.4 — update profile with theme/currency validation.</summary>
public class UpdateProfileHandlerTests
{
    [Fact]
    public async Task Update_persists_preferences()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new UpdateProfileCommand(
            FullName: "Ada Lovelace",
            Locale: "en",
            MainCurrency: "USD",
            MonthlyBudget: 120.50m,
            ApplicationThemeColor: ThemeColors.ForestGreen,
            DarkTheme: true));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
        Assert.Equal("en", result.Value.Locale);
        Assert.Equal("USD", result.Value.MainCurrency);
        Assert.Equal(120.50m, result.Value.MonthlyBudget);
        Assert.Equal(ThemeColors.ForestGreen, result.Value.ApplicationThemeColor);
        Assert.True(result.Value.DarkTheme);

        using var scope = harness.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(userId.ToString());
        Assert.Equal("en", user!.Locale);
        Assert.Equal("USD", user.MainCurrency);

        var log = await scope.ServiceProvider.GetRequiredService<SubifyDbContext>()
            .ActivityLogs.SingleAsync(a => a.UserId == userId);
        Assert.Equal(ActivityLogConstants.EntityTypes.Profile, log.EntityType);
        Assert.Equal(ActivityLogConstants.Actions.ProfileUpdated, log.Action);
        Assert.Equal(userId, log.EntityId);
        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("USD", log.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_null_budget_clears_budget()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", budget: 99m);
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new UpdateProfileCommand(
            "Name", "tr", "TRY", null, ThemeColors.Default, false));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Null(result.Value.MonthlyBudget);
    }

    [Fact]
    public async Task Update_rejects_invalid_theme_and_currency()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var badTheme = await harness.HandleAsync(new UpdateProfileCommand(
            "N", "tr", "TRY", null, "Neon Pink", false));
        Assert.Equal(DomainErrors.ProfileErrors.InvalidTheme.Code, badTheme.Error.Code);

        var badCurrency = await harness.HandleAsync(new UpdateProfileCommand(
            "N", "tr", "JPY", null, ThemeColors.Default, false));
        Assert.Equal(DomainErrors.ProfileErrors.InvalidCurrency.Code, badCurrency.Error.Code);

        var badLocale = await harness.HandleAsync(new UpdateProfileCommand(
            "N", "de", "TRY", null, ThemeColors.Default, false));
        Assert.Equal(DomainErrors.ProfileErrors.InvalidLocale.Code, badLocale.Error.Code);
    }

    [Fact]
    public async Task Update_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync(new UpdateProfileCommand(
            "N", "tr", "TRY", null, ThemeColors.Default, false));
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
            services.AddHttpContextAccessor();
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
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<UpdateProfileHandler>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<SubifyDbContext>().Database.EnsureCreatedAsync();
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

        public void SetUser(Guid userId)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = [AppRoles.User];
        }

        public async Task<Guid> SeedUserAsync(string email, decimal? budget = null)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            if (budget is not null)
            {
                user.UpdateProfile(monthlyBudget: budget);
            }

            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            await users.AddToRoleAsync(user, AppRoles.User);
            return user.Id;
        }

        public async Task<Result<ProfileResponse>> HandleAsync(UpdateProfileCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateProfileHandler>()
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
