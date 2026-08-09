using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.GetSubscriptionById;
using Subify.Application.Features.Subscriptions.ListSubscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.10 — nested category/provider on subscription DTOs.</summary>
public class SubscriptionResponseNestedTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Get_and_list_include_nested_category_and_provider()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("owner@subify.local");
        harness.SetUser(userId);

        var providerId = await harness.SeedProviderAsync("Netflix Catalog", "netflix-catalog");
        var categoryId = await harness.SeedCategoryAsync("streaming", "play-circle", "#E50914");

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            Name: "Netflix",
            Price: 149.99m,
            Currency: "TRY",
            BillingCycle: "monthly",
            SharedWithCount: 4,
            NextRenewalDate: Today.AddDays(10),
            ProviderId: providerId,
            CategoryId: categoryId));

        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);
        Assert.NotNull(created.Value.Category);
        Assert.Equal("streaming", created.Value.Category!.Slug);
        Assert.Equal("streaming", created.Value.Category.Name);
        Assert.False(created.Value.Category.IsUserCategory);
        Assert.Equal("play-circle", created.Value.Category.Icon);
        Assert.NotNull(created.Value.Provider);
        Assert.Equal("Netflix Catalog", created.Value.Provider!.Name);
        Assert.Equal("netflix-catalog", created.Value.Provider.Slug);
        Assert.Equal(37.50m, created.Value.UserShare);

        var get = await harness.GetAsync(created.Value.Id);
        Assert.True(get.IsSuccess);
        Assert.Equal(categoryId, get.Value.Category!.Id);
        Assert.Equal(providerId, get.Value.Provider!.Id);
        Assert.Equal(37.50m, get.Value.UserShare);

        var list = await harness.ListAsync();
        Assert.True(list.IsSuccess);
        var item = Assert.Single(list.Value.Data);
        Assert.NotNull(item.Category);
        Assert.NotNull(item.Provider);
        Assert.Equal("streaming", item.Category!.Slug);
    }

    [Fact]
    public async Task Nested_user_category_is_flagged()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var userCatId = await harness.SeedUserCategoryAsync(userId, "My Gym", "dumbbell", "#111");

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Gym",
            50m,
            "TRY",
            "monthly",
            1,
            Today.AddDays(5),
            UserCategoryId: userCatId));

        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);
        Assert.NotNull(created.Value.Category);
        Assert.True(created.Value.Category!.IsUserCategory);
        Assert.Equal("My Gym", created.Value.Category.Name);
        Assert.Null(created.Value.Category.Slug);
        Assert.Null(created.Value.Provider);
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
            services.AddScoped<IExchangeRateLookup, Subify.Infrastructure.ExchangeRates.ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<GetSubscriptionByIdHandler>();
            services.AddScoped<ListSubscriptionsHandler>();

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

        public async Task<Guid> SeedProviderAsync(string name, string slug)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var p = Provider.CreateCatalog(name, slug, "TRY", 99m, BillingCycle.Monthly, "TR");
            db.Providers.Add(p);
            await db.SaveChangesAsync();
            return p.Id;
        }

        public async Task<Guid> SeedCategoryAsync(string slug, string? icon, string? color)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = Category.CreateSystem(slug, icon, color, 1);
            db.Categories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Guid> SeedUserCategoryAsync(Guid userId, string name, string? icon, string? color)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name, icon, color);
            db.UserCategories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Result<CreateSubscriptionResponse>> CreateAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<Application.Features.Subscriptions.SubscriptionResponse>> GetAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetSubscriptionByIdHandler>()
                .Handle(new GetSubscriptionByIdQuery(id), CancellationToken.None);
        }

        public async Task<Result<ListSubscriptionsResponse>> ListAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListSubscriptionsHandler>()
                .Handle(new ListSubscriptionsQuery(), CancellationToken.None);
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
