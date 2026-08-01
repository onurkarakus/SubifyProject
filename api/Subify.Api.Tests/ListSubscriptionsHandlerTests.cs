using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.ListSubscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.4 — ListSubscriptions filters / pagination / search.</summary>
public class ListSubscriptionsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task List_returns_only_current_user_active_by_default()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        harness.SetUser(owner);
        await harness.CreateAsync("Netflix", 100m);
        await harness.CreateAsync("Spotify", 50m);
        var archived = await harness.CreateAsync("Old Gym", 30m);
        await harness.ArchiveAsync(archived.Value.Id);

        harness.SetUser(other);
        await harness.CreateAsync("Other Sub", 99m);

        harness.SetUser(owner);
        var list = await harness.ListAsync(new ListSubscriptionsQuery());

        Assert.True(list.IsSuccess, list.IsFailure ? list.Error.Code : null);
        Assert.Equal(2, list.Value.Pagination.TotalItems);
        Assert.Equal(2, list.Value.Data.Count);
        Assert.DoesNotContain(list.Value.Data, s => s.Name == "Old Gym");
        Assert.DoesNotContain(list.Value.Data, s => s.Name == "Other Sub");
    }

    [Fact]
    public async Task List_includeArchived_returns_archived_rows()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var active = await harness.CreateAsync("Active", 10m);
        var archived = await harness.CreateAsync("Archived", 20m);
        await harness.ArchiveAsync(archived.Value.Id);

        var list = await harness.ListAsync(new ListSubscriptionsQuery(IncludeArchived: true));
        Assert.Equal(2, list.Value.Pagination.TotalItems);
        Assert.Contains(list.Value.Data, s => s.Id == active.Value.Id && !s.Archived);
        Assert.Contains(list.Value.Data, s => s.Id == archived.Value.Id && s.Archived);
    }

    [Fact]
    public async Task List_filters_by_category_slug_and_search()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var streamingId = await harness.SeedCategoryAsync("streaming");
        var musicId = await harness.SeedCategoryAsync("music");

        await harness.CreateAsync(
            new CreateSubscriptionCommand(
                "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5), CategoryId: streamingId));
        await harness.CreateAsync(
            new CreateSubscriptionCommand(
                "Spotify", 50m, "TRY", "monthly", 1, Today.AddDays(3), CategoryId: musicId));
        await harness.CreateAsync(
            new CreateSubscriptionCommand(
                "Disney+", 80m, "TRY", "monthly", 1, Today.AddDays(7), CategoryId: streamingId));

        var bySlug = await harness.ListAsync(new ListSubscriptionsQuery(Category: "streaming"));
        Assert.Equal(2, bySlug.Value.Pagination.TotalItems);
        Assert.All(bySlug.Value.Data, s => Assert.Equal(streamingId, s.CategoryId));

        var search = await harness.ListAsync(new ListSubscriptionsQuery(Search: "spot"));
        Assert.Equal(1, search.Value.Pagination.TotalItems);
        Assert.Equal("Spotify", search.Value.Data[0].Name);
    }

    [Fact]
    public async Task List_paginates()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        for (var i = 1; i <= 5; i++)
        {
            await harness.CreateAsync($"Sub {i}", 10m + i, renewalOffsetDays: i);
        }

        var page1 = await harness.ListAsync(new ListSubscriptionsQuery(Page: 1, PageSize: 2));
        Assert.Equal(5, page1.Value.Pagination.TotalItems);
        Assert.Equal(3, page1.Value.Pagination.TotalPages);
        Assert.Equal(2, page1.Value.Data.Count);

        var page3 = await harness.ListAsync(new ListSubscriptionsQuery(Page: 3, PageSize: 2));
        Assert.Single(page3.Value.Data);
    }

    [Fact]
    public async Task List_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.ListAsync(new ListSubscriptionsQuery());
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
    }

    [Fact]
    public async Task List_summary_uses_active_main_currency_totals_not_page()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("sum@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        // Monthly 100 TRY
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5)));
        // Yearly 1200 TRY → 100 / mo
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Annual Cloud", 1200m, "TRY", "yearly", 1, Today.AddDays(10)));
        // Shared 40 / 2 = 20 TRY monthly
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Shared Gym", 40m, "TRY", "monthly", 2, Today.AddDays(3)));
        // USD without rate → excluded from total + warning (4.3.4)
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "USD Tool", 50m, "USD", "monthly", 1, Today.AddDays(4)));
        // Archived ignored in summary
        var archived = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Old", 999m, "TRY", "monthly", 1, Today.AddDays(2)));
        await harness.ArchiveAsync(archived.Value.Id);

        var page = await harness.ListAsync(new ListSubscriptionsQuery(Page: 1, PageSize: 1));
        Assert.True(page.IsSuccess, page.IsFailure ? page.Error.Code : null);
        Assert.Single(page.Value.Data);
        Assert.Equal("TRY", page.Value.Summary.Currency);
        // 100 + 100 + 20 = 220 monthly; yearly 1200 + 1200 + 240 = 2640
        Assert.Equal(220m, page.Value.Summary.MonthlyTotal);
        Assert.Equal(2640m, page.Value.Summary.YearlyTotal);
        Assert.True(page.Value.Summary.HasUnconvertedAmounts);
        Assert.NotEmpty(page.Value.Summary.Warnings);

        // includeArchived does not inflate summary
        var withArchived = await harness.ListAsync(new ListSubscriptionsQuery(IncludeArchived: true));
        Assert.Equal(220m, withArchived.Value.Summary.MonthlyTotal);
    }

    [Fact]
    public async Task List_summary_converts_foreign_currency_with_snapshot_rate()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("fx@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        await harness.SeedRateAsync("USD", "TRY", 30m);
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Local", 100m, "TRY", "monthly", 1, Today.AddDays(3)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "US Tool", 10m, "USD", "monthly", 1, Today.AddDays(4)));

        var list = await harness.ListAsync(new ListSubscriptionsQuery());
        Assert.True(list.IsSuccess, list.IsFailure ? list.Error.Code : null);
        // 100 TRY + 10*30 = 400
        Assert.Equal(400m, list.Value.Summary.MonthlyTotal);
        Assert.False(list.Value.Summary.HasUnconvertedAmounts);
        Assert.Empty(list.Value.Summary.Warnings);
    }

    [Fact]
    public async Task List_summary_sets_budget_exceeded_flag()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("budget@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        await harness.SetMonthlyBudgetAsync(userId, 150m);
        harness.SetUser(userId);

        // monthly total 200 > budget 150
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "A", 100m, "TRY", "monthly", 1, Today.AddDays(3)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "B", 100m, "TRY", "monthly", 1, Today.AddDays(4)));

        var list = await harness.ListAsync(new ListSubscriptionsQuery());
        Assert.True(list.IsSuccess, list.IsFailure ? list.Error.Code : null);
        Assert.Equal(200m, list.Value.Summary.MonthlyTotal);
        Assert.Equal(150m, list.Value.Summary.MonthlyBudget);
        Assert.True(list.Value.Summary.IsBudgetExceeded);

        await harness.SetMonthlyBudgetAsync(userId, 500m);
        var under = await harness.ListAsync(new ListSubscriptionsQuery());
        Assert.False(under.Value.Summary.IsBudgetExceeded);
        Assert.Equal(500m, under.Value.Summary.MonthlyBudget);

        await harness.SetMonthlyBudgetAsync(userId, null);
        var off = await harness.ListAsync(new ListSubscriptionsQuery());
        Assert.Null(off.Value.Summary.MonthlyBudget);
        Assert.False(off.Value.Summary.IsBudgetExceeded);
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

        public async Task<Guid> SeedCategoryAsync(string slug)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = Category.CreateSystem(slug, null, null, 1);
            db.Categories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public Task<Result<CreateSubscriptionResponse>> CreateAsync(
            string name,
            decimal price,
            int renewalOffsetDays = 5) =>
            CreateAsync(new CreateSubscriptionCommand(
                name, price, "TRY", "monthly", 1, Today.AddDays(renewalOffsetDays)));

        public async Task<Result<CreateSubscriptionResponse>> CreateAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ListSubscriptionsResponse>> ListAsync(ListSubscriptionsQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListSubscriptionsHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task ArchiveAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.IgnoreQueryFilters().SingleAsync(s => s.Id == id);
            sub.Archive();
            await db.SaveChangesAsync();
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

        public async Task SetMonthlyBudgetAsync(Guid userId, decimal? budget)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString())
                       ?? throw new InvalidOperationException("user missing");
            user.MonthlyBudget = budget is > 0 ? budget : null;
            await users.UpdateAsync(user);
        }

        public async Task SeedRateAsync(string from, string to, decimal rate)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot(
                from, to, rate, "test", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
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
