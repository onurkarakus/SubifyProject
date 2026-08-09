using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.UpcomingSubscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.9 — UpcomingSubscriptions days / overdue flags.</summary>
public class UpcomingSubscriptionsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Upcoming_includes_window_and_overdue_excludes_far_future()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        // Within 7 days
        var soon = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Soon", 100m, "TRY", "monthly", 1, Today.AddDays(2)));
        // Outside window
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Later", 50m, "TRY", "monthly", 1, Today.AddDays(30)));
        // Overdue via domain update
        var late = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Late", 40m, "TRY", "monthly", 2, Today.AddDays(5)));
        await harness.SetRenewalAsync(late.Value.Id, Today.AddDays(-3));

        // Archived must not appear
        var archived = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Archived Soon", 10m, "TRY", "monthly", 1, Today.AddDays(1)));
        await harness.ArchiveDirectAsync(archived.Value.Id);

        var result = await harness.UpcomingAsync(new UpcomingSubscriptionsQuery(Days: 7));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);

        Assert.Equal(2, result.Value.Data.Count);
        Assert.Contains(result.Value.Data, i => i.Id == soon.Value.Id && i.IsUpcoming && !i.IsOverdue);
        Assert.Contains(result.Value.Data, i => i.Id == late.Value.Id && i.IsOverdue && !i.IsUpcoming);
        Assert.DoesNotContain(result.Value.Data, i => i.Name == "Later");
        Assert.DoesNotContain(result.Value.Data, i => i.Name == "Archived Soon");

        var lateItem = result.Value.Data.Single(i => i.Id == late.Value.Id);
        Assert.Equal(-3, lateItem.DaysUntilRenewal);
        Assert.Equal(20m, lateItem.UserShare); // 40/2

        var soonItem = result.Value.Data.Single(i => i.Id == soon.Value.Id);
        Assert.Equal(2, soonItem.DaysUntilRenewal);

        Assert.Equal(1, result.Value.OverdueCount);
        Assert.Equal(1, result.Value.UpcomingCount);
        // Total userShare in TRY: 100 + 20
        Assert.Equal(120m, result.Value.Total);
        Assert.Equal("TRY", result.Value.Currency);
        // Overdue first (earlier renewal date)
        Assert.Equal(late.Value.Id, result.Value.Data[0].Id);
    }

    [Fact]
    public async Task Upcoming_respects_days_window()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        await harness.CreateAsync(new CreateSubscriptionCommand(
            "In3", 10m, "TRY", "monthly", 1, Today.AddDays(3)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "In10", 10m, "TRY", "monthly", 1, Today.AddDays(10)));

        var narrow = await harness.UpcomingAsync(new UpcomingSubscriptionsQuery(Days: 5));
        Assert.Single(narrow.Value.Data);
        Assert.Equal("In3", narrow.Value.Data[0].Name);

        var wide = await harness.UpcomingAsync(new UpcomingSubscriptionsQuery(Days: 14));
        Assert.Equal(2, wide.Value.Data.Count);
    }

    [Fact]
    public async Task Upcoming_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.UpcomingAsync(new UpcomingSubscriptionsQuery());
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
            services.AddScoped<UpcomingSubscriptionsHandler>();

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

        public async Task SetMainCurrencyAsync(Guid userId, string currency)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString())
                       ?? throw new InvalidOperationException("user missing");
            user.MainCurrency = currency;
            await users.UpdateAsync(user);
        }

        public async Task<Result<CreateSubscriptionResponse>> CreateAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task SetRenewalAsync(Guid id, DateOnly renewal)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.SingleAsync(s => s.Id == id);
            var update = sub.Update(
                sub.Name,
                sub.Price,
                sub.Currency,
                sub.BillingCycle,
                sub.SharedWithCount,
                renewal,
                sub.ProviderId,
                sub.CategoryId,
                sub.UserCategoryId,
                sub.Notes,
                today: Today);
            if (update.IsFailure)
            {
                throw new InvalidOperationException(update.Error.Code);
            }

            await db.SaveChangesAsync();
        }

        public async Task ArchiveDirectAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.SingleAsync(s => s.Id == id);
            sub.Archive();
            await db.SaveChangesAsync();
        }

        public async Task<Result<UpcomingSubscriptionsResponse>> UpcomingAsync(UpcomingSubscriptionsQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpcomingSubscriptionsHandler>()
                .Handle(query, CancellationToken.None);
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
