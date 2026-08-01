using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.UpdateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.6 — UpdateSubscription ownership + activity old/new.</summary>
public class UpdateSubscriptionHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Update_own_subscription_persists_and_logs_activity()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("owner@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(10)));
        Assert.True(created.IsSuccess);

        var updated = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            Id: created.Value.Id,
            Name: "Netflix Premium",
            Price: 200m,
            Currency: "TRY",
            BillingCycle: "yearly",
            SharedWithCount: 2,
            NextRenewalDate: Today.AddDays(30),
            Notes: "Upgraded"));

        Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Code : null);
        Assert.Equal("Netflix Premium", updated.Value.Name);
        Assert.Equal(200m, updated.Value.Price);
        Assert.Equal(BillingCycle.Yearly, updated.Value.BillingCycle);
        Assert.Equal(100m, updated.Value.UserShare); // 200/2
        Assert.Equal("Upgraded", updated.Value.Notes);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var log = await db.ActivityLogs.SingleAsync(a =>
            a.EntityId == created.Value.Id
            && a.Action == ActivityLogConstants.Actions.SubscriptionUpdated);

        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("Netflix", log.OldValues, StringComparison.Ordinal);
        Assert.True(
            log.NewValues.Contains("Netflix Premium", StringComparison.Ordinal)
            || log.NewValues.Contains("Premium", StringComparison.Ordinal));
        Assert.Contains("200", log.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            Guid.CreateVersion7(), "X", 10m, "TRY", "monthly", 1, Today.AddDays(1)));

        Assert.Equal(DomainErrors.Subscription.SubscriptionNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_foreign_returns_access_denied()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        harness.SetUser(owner);
        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Private", 50m, "TRY", "monthly", 1, Today.AddDays(5)));

        harness.SetUser(other);
        var result = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            created.Value.Id, "Hacked", 1m, "TRY", "monthly", 1, Today.AddDays(5)));

        Assert.Equal(DomainErrors.Subscription.SubscriptionAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_rejects_inactive_provider()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Tool", 10m, "USD", "monthly", 1, Today.AddDays(5)));
        var inactiveProvider = await harness.SeedProviderAsync(active: false);

        var result = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            created.Value.Id,
            "Tool",
            10m,
            "USD",
            "monthly",
            1,
            Today.AddDays(5),
            ProviderId: inactiveProvider));

        Assert.Equal(DomainErrors.Subscription.ProviderNotActive.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_rejects_foreign_user_category()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");
        var foreignCat = await harness.SeedUserCategoryAsync(other, "Not yours");

        harness.SetUser(owner);
        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Gym", 50m, "TRY", "monthly", 1, Today.AddDays(5)));

        var result = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            created.Value.Id,
            "Gym",
            50m,
            "TRY",
            "monthly",
            1,
            Today.AddDays(5),
            UserCategoryId: foreignCat));

        Assert.Equal(DomainErrors.Subscription.SubscriptionAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.UpdateAsync(new UpdateSubscriptionCommand(
            Guid.CreateVersion7(), "X", 10m, "TRY", "monthly", 1, Today.AddDays(1)));
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
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<UpdateSubscriptionHandler>();

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

        public async Task<Guid> SeedUserCategoryAsync(Guid userId, string name)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name);
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

        public async Task<Result<SubscriptionResponse>> UpdateAsync(UpdateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateSubscriptionHandler>()
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
