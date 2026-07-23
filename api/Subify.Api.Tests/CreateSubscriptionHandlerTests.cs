using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.1 — CreateSubscription command/handler.</summary>
public class CreateSubscriptionHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Create_persists_subscription_for_current_user()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var userId = await harness.SeedUserAsync("owner@subify.local");
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            Name: "Netflix",
            Price: 149.99m,
            Currency: "try",
            BillingCycle: "monthly",
            SharedWithCount: 2,
            NextRenewalDate: Today.AddDays(14),
            Notes: "Family plan"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("Netflix", result.Value.Name);
        Assert.Equal("TRY", result.Value.Currency);
        Assert.Equal(BillingCycle.Monthly, result.Value.BillingCycle);
        Assert.Equal(75.00m, result.Value.UserShare); // 149.99 / 2
        Assert.Equal(900.00m, result.Value.YearlyEquivalentShare);
        Assert.False(result.Value.Archived);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var row = await db.Subscriptions.SingleAsync(s => s.Id == result.Value.Id);
        Assert.Equal(userId, row.UserId);
        Assert.Equal("Family plan", row.Notes);
    }

    [Fact]
    public async Task Create_rejects_unauthenticated()
    {
        await using var harness = await CreateHarness.CreateAsync();

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "X", 10m, "TRY", "monthly", 1, Today.AddDays(1)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_past_renewal_and_invalid_cycle()
    {
        await using var harness = await CreateHarness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var past = await harness.HandleAsync(new CreateSubscriptionCommand(
            "Old", 10m, "TRY", "monthly", 1, Today.AddDays(-1)));
        Assert.Equal(DomainErrors.Subscription.InvalidRenewalDate.Code, past.Error.Code);

        var cycle = await harness.HandleAsync(new CreateSubscriptionCommand(
            "Bad", 10m, "TRY", "weekly", 1, Today.AddDays(1)));
        Assert.Equal(DomainErrors.Subscription.InvalidBillingCycle.Code, cycle.Error.Code);
    }

    [Fact]
    public async Task Create_with_inactive_provider_fails()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var providerId = await harness.SeedProviderAsync(active: false);

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "From provider",
            20m,
            "USD",
            "yearly",
            1,
            Today.AddDays(30),
            ProviderId: providerId));

        Assert.Equal(DomainErrors.Subscription.ProviderNotActive.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_with_active_provider_and_system_category()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var providerId = await harness.SeedProviderAsync(active: true);
        var categoryId = await harness.SeedSystemCategoryAsync();

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "Spotify",
            59.99m,
            "TRY",
            "Monthly",
            1,
            Today.AddDays(7),
            ProviderId: providerId,
            CategoryId: categoryId));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(providerId, result.Value.ProviderId);
        Assert.Equal(categoryId, result.Value.CategoryId);
    }

    [Fact]
    public async Task Create_rejects_foreign_user_category()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");
        var foreignCategoryId = await harness.SeedUserCategoryAsync(other, "Other cat");

        harness.SetUser(owner);

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "Gym",
            50m,
            "TRY",
            "monthly",
            1,
            Today.AddDays(5),
            UserCategoryId: foreignCategoryId));

        Assert.Equal(DomainErrors.Subscription.SubscriptionAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_missing_or_inactive_system_category()
    {
        await using var harness = await CreateHarness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var missing = await harness.HandleAsync(new CreateSubscriptionCommand(
            "X", 10m, "TRY", "monthly", 1, Today.AddDays(1),
            CategoryId: Guid.CreateVersion7()));
        Assert.Equal(DomainErrors.Subscription.CategoryNotFound.Code, missing.Error.Code);

        var inactiveId = await harness.SeedSystemCategoryAsync(active: false);
        var inactive = await harness.HandleAsync(new CreateSubscriptionCommand(
            "X", 10m, "TRY", "monthly", 1, Today.AddDays(1),
            CategoryId: inactiveId));
        Assert.Equal(DomainErrors.Subscription.CategoryNotFound.Code, inactive.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_missing_user_category()
    {
        await using var harness = await CreateHarness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "X", 10m, "TRY", "monthly", 1, Today.AddDays(1),
            UserCategoryId: Guid.CreateVersion7()));

        Assert.Equal(DomainErrors.Subscription.CategoryNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_with_own_user_category_succeeds()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var ownCat = await harness.SeedUserCategoryAsync(userId, "Mine");

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            "Gym", 50m, "TRY", "monthly", 1, Today.AddDays(5),
            UserCategoryId: ownCat));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(ownCat, result.Value.UserCategoryId);
        Assert.Null(result.Value.CategoryId);
    }

    [Fact]
    public async Task Create_writes_subscription_created_activity_log()
    {
        await using var harness = await CreateHarness.CreateAsync();
        var userId = await harness.SeedUserAsync("audit@subify.local");
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new CreateSubscriptionCommand(
            Name: "Disney+",
            Price: 100m,
            Currency: "TRY",
            BillingCycle: "monthly",
            SharedWithCount: 1,
            NextRenewalDate: Today.AddDays(10)));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var log = await db.ActivityLogs.SingleAsync(a => a.UserId == userId);

        Assert.Equal(ActivityLogConstants.EntityTypes.Subscription, log.EntityType);
        Assert.Equal(ActivityLogConstants.Actions.SubscriptionCreated, log.Action);
        Assert.Equal(result.Value.Id, log.EntityId);
        Assert.Contains("Disney+", log.Description);
        Assert.NotNull(log.NewValues);
        Assert.Contains("\"name\"", log.NewValues);
        // JsonSerializer may escape '+' as \u002B
        Assert.True(
            log.NewValues.Contains("Disney+", StringComparison.Ordinal)
            || log.NewValues.Contains("Disney\\u002B", StringComparison.Ordinal));
        Assert.Null(log.OldValues);
    }

    private sealed class CreateHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private CreateHarness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public static async Task<CreateHarness> CreateAsync()
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

            services.AddHttpContextAccessor();
            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<CreateSubscriptionHandler>();

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

            return new CreateHarness(connection, provider);
        }

        public void SetUser(Guid userId)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = [AppRoles.User];
        }

        public async Task<Guid> SeedUserAsync(string email)
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

            await users.AddToRoleAsync(user, AppRoles.User);
            return user.Id;
        }

        public async Task<Guid> SeedProviderAsync(bool active)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var p = Provider.CreateCatalog(
                "Test Provider",
                $"provider-{Guid.NewGuid():N}"[..20],
                "USD",
                9.99m,
                BillingCycle.Monthly,
                "GLOBAL");
            if (!active)
            {
                p.Deactivate();
            }

            db.Providers.Add(p);
            await db.SaveChangesAsync();
            return p.Id;
        }

        public async Task<Guid> SeedSystemCategoryAsync(bool active = true)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = Category.CreateSystem(
                $"cat-{Guid.NewGuid():N}"[..12],
                "tv",
                "#fff",
                1);
            if (!active)
            {
                c.Deactivate();
            }

            db.Categories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Guid> SeedUserCategoryAsync(Guid userId, string name)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name);
            db.UserCategories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Result<CreateSubscriptionResponse>> HandleAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
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
