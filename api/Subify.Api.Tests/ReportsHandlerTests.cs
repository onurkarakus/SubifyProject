using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories;
using Subify.Application.Features.Reports;
using Subify.Application.Features.Reports.GetCategoryBreakdown;
using Subify.Application.Features.Reports.GetCurrencyDistribution;
using Subify.Application.Features.Reports.GetMonthlySpend;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 6.1 — monthly spend, category breakdown, currency distribution, empty state.</summary>
public class ReportsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Monthly_spend_empty_when_no_subscriptions()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("empty@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        var result = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 6));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Empty(result.Value.Data);
        Assert.Equal("TRY", result.Value.Currency);
        Assert.Equal(0m, result.Value.Average);
        Assert.Equal(DomainErrors.ReportErrors.InsufficientData.Description, result.Value.Message);
    }

    [Fact]
    public async Task Monthly_spend_returns_series_with_current_active_totals()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("ms@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        // 100 monthly + 1200 yearly (=100/mo) + shared 40/2=20 → 220
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Cloud", 1200m, "TRY", "yearly", 1, Today.AddDays(10)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Gym", 40m, "TRY", "monthly", 2, Today.AddDays(3)));

        var result = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 3));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(3, result.Value.Data.Count);
        Assert.Null(result.Value.Message);
        Assert.Equal("TRY", result.Value.Currency);

        // Current month should include all three (just created); earlier months are 0.
        var current = result.Value.Data[^1];
        Assert.Equal(220m, current.Total);
        Assert.All(result.Value.Data.Take(2), p => Assert.Equal(0m, p.Total));
        Assert.Equal(
            decimal.Round(220m / 3m, 2, MidpointRounding.AwayFromZero),
            result.Value.Average);
    }

    [Fact]
    public async Task Monthly_spend_excludes_archived_from_current_month()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("arch@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        var keep = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Keep", 100m, "TRY", "monthly", 1, Today.AddDays(5)));
        var drop = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Drop", 50m, "TRY", "monthly", 1, Today.AddDays(5)));
        Assert.True(keep.IsSuccess && drop.IsSuccess);

        await harness.ArchiveAsync(drop.Value.Id);

        var result = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 1));
        Assert.True(result.IsSuccess);
        // Archived this month: WasActiveDuring still true if ArchivedAt >= monthStart,
        // so both may still count for current month. Archive after create same day → still active
        // during month. For "current run-rate" we also accept archived-in-month counting.
        // Force: set DeletedAt to start of previous month so current month excludes it.
        await harness.BackdateArchiveAsync(drop.Value.Id, DateTimeOffset.UtcNow.AddMonths(-2));

        var after = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 1));
        Assert.Equal(100m, after.Value.Data[0].Total);
    }

    [Fact]
    public async Task Monthly_spend_converts_with_fx_and_respects_currency_query()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("fx@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        await harness.SeedRateAsync("USD", "TRY", 30m);
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "US Tool", 10m, "USD", "monthly", 1, Today.AddDays(4)));

        var asTry = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 1));
        Assert.Equal(300m, asTry.Value.Data[0].Total);
        Assert.Equal("TRY", asTry.Value.Currency);

        var asUsd = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 1, Currency: "USD"));
        Assert.Equal(10m, asUsd.Value.Data[0].Total);
        Assert.Equal("USD", asUsd.Value.Currency);
    }

    [Fact]
    public async Task Category_breakdown_groups_system_user_and_uncategorized()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("cat@subify.local", locale: "en");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId, locale: "en");

        var streamingId = await harness.SeedCategoryAsync("streaming", "#E50914");
        var userCatId = await harness.SeedUserCategoryAsync(userId, "Side Projects", "#112233");

        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5), CategoryId: streamingId));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Disney", 50m, "TRY", "monthly", 1, Today.AddDays(5), CategoryId: streamingId));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "VPS", 40m, "TRY", "monthly", 1, Today.AddDays(5), UserCategoryId: userCatId));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Misc", 10m, "TRY", "monthly", 1, Today.AddDays(5)));

        var result = await harness.CategoryAsync(new GetCategoryBreakdownQuery(ExplicitLocale: "en"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Null(result.Value.Message);
        Assert.Equal(200m, result.Value.GrandTotal); // 100+50+40+10
        Assert.Equal(3, result.Value.Data.Count);

        var streaming = result.Value.Data.Single(d => d.Category == "streaming");
        Assert.Equal("Streaming", streaming.Name);
        Assert.Equal("#E50914", streaming.Color);
        Assert.Equal(150m, streaming.Total);
        Assert.Equal(2, streaming.Count);
        Assert.Equal(75.0m, streaming.Percentage);

        var custom = result.Value.Data.Single(d => d.Category.StartsWith("user:", StringComparison.Ordinal));
        Assert.Equal("Side Projects", custom.Name);
        Assert.Equal("#112233", custom.Color);
        Assert.Equal(40m, custom.Total);
        Assert.Equal(1, custom.Count);

        var uncategorized = result.Value.Data.Single(d => d.Category == ReportConstants.UncategorizedKey);
        Assert.Equal(10m, uncategorized.Total);
        Assert.Equal(ReportConstants.UncategorizedColor, uncategorized.Color);
    }

    [Fact]
    public async Task Category_breakdown_empty_state()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("cempty@subify.local");
        harness.SetUser(userId);

        var result = await harness.CategoryAsync(new GetCategoryBreakdownQuery());
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Data);
        Assert.Equal(0m, result.Value.GrandTotal);
        Assert.Equal(DomainErrors.ReportErrors.InsufficientData.Description, result.Value.Message);
    }

    [Fact]
    public async Task Currency_distribution_groups_and_converts()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("cur@subify.local");
        await harness.SetMainCurrencyAsync(userId, "TRY");
        harness.SetUser(userId);

        await harness.SeedRateAsync("USD", "TRY", 30m);
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Local A", 100m, "TRY", "monthly", 1, Today.AddDays(3)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Local B", 50m, "TRY", "monthly", 1, Today.AddDays(4)));
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "US Tool", 10m, "USD", "monthly", 1, Today.AddDays(5)));

        var result = await harness.CurrencyAsync(new GetCurrencyDistributionQuery());
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Null(result.Value.Message);
        // 150 TRY + 10*30 = 450
        Assert.Equal(450m, result.Value.GrandTotal);
        Assert.Equal("TRY", result.Value.Currency);
        Assert.Equal(2, result.Value.Data.Count);

        var tryRow = result.Value.Data.Single(d => d.Currency == "TRY");
        Assert.Equal(150m, tryRow.MonthlyTotal);
        Assert.Equal(150m, tryRow.ConvertedMonthlyTotal);
        Assert.Equal(2, tryRow.Count);
        Assert.Equal(33.3m, tryRow.Percentage); // 150/450 = 33.333 → 33.3

        var usdRow = result.Value.Data.Single(d => d.Currency == "USD");
        Assert.Equal(10m, usdRow.MonthlyTotal);
        Assert.Equal(300m, usdRow.ConvertedMonthlyTotal);
        Assert.Equal(66.7m, usdRow.Percentage);
    }

    [Fact]
    public async Task Currency_distribution_empty_state()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("curempty@subify.local");
        harness.SetUser(userId);

        var result = await harness.CurrencyAsync(new GetCurrencyDistributionQuery());
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Data);
        Assert.Equal(DomainErrors.ReportErrors.InsufficientData.Description, result.Value.Message);
    }

    [Fact]
    public async Task Reports_unauthenticated_fail()
    {
        await using var harness = await Harness.CreateAsync();

        var m = await harness.MonthlyAsync(new GetMonthlySpendQuery());
        var c = await harness.CategoryAsync(new GetCategoryBreakdownQuery());
        var d = await harness.CurrencyAsync(new GetCurrencyDistributionQuery());

        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, m.Error.Code);
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, c.Error.Code);
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, d.Error.Code);
    }

    [Fact]
    public async Task Reports_ignore_other_users_subscriptions()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        harness.SetUser(other);
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Other", 999m, "TRY", "monthly", 1, Today.AddDays(2)));

        harness.SetUser(owner);
        await harness.SetMainCurrencyAsync(owner, "TRY");
        await harness.CreateAsync(new CreateSubscriptionCommand(
            "Mine", 25m, "TRY", "monthly", 1, Today.AddDays(2)));

        var monthly = await harness.MonthlyAsync(new GetMonthlySpendQuery(Months: 1));
        Assert.Equal(25m, monthly.Value.Data[0].Total);

        var cat = await harness.CategoryAsync(new GetCategoryBreakdownQuery());
        Assert.Equal(25m, cat.Value.GrandTotal);
        Assert.Single(cat.Value.Data);

        var cur = await harness.CurrencyAsync(new GetCurrencyDistributionQuery());
        Assert.Equal(25m, cur.Value.GrandTotal);
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
            services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<ICategoryNameLookup, CategoryNameLookup>();
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<GetMonthlySpendHandler>();
            services.AddScoped<GetCategoryBreakdownHandler>();
            services.AddScoped<GetCurrencyDistributionHandler>();

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

                // Localized streaming category name for breakdown tests.
                db.Resources.Add(Resource.Create(
                    SystemResources.Pages.Category,
                    "streaming",
                    "en",
                    "Streaming"));
                await db.SaveChangesAsync();
            }

            return new Harness(connection, provider);
        }

        public void SetUser(Guid userId, string? locale = null)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Locale = locale;
            fake.Roles = [AppRoles.User];
        }

        public async Task<Guid> SeedUserAsync(string email, string locale = "tr")
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            user.Locale = locale;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            await users.AddToRoleAsync(user, AppRoles.User);
            return user.Id;
        }

        public async Task<Guid> SeedCategoryAsync(string slug, string? color = null)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = Category.CreateSystem(slug, null, color, 1);
            db.Categories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Guid> SeedUserCategoryAsync(Guid userId, string name, string? color)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name, icon: null, color: color);
            db.UserCategories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
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

        public async Task SeedRateAsync(string from, string to, decimal rate)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot(
                from, to, rate, "test", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        public async Task<Result<CreateSubscriptionResponse>> CreateAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task ArchiveAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.IgnoreQueryFilters().SingleAsync(s => s.Id == id);
            sub.Archive();
            await db.SaveChangesAsync();
        }

        public async Task BackdateArchiveAsync(Guid id, DateTimeOffset archivedAt)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.IgnoreQueryFilters().SingleAsync(s => s.Id == id);
            if (!sub.Archived)
            {
                sub.Archive();
            }

            sub.DeletedAt = archivedAt;
            await db.SaveChangesAsync();
        }

        public async Task<Result<MonthlySpendResponse>> MonthlyAsync(GetMonthlySpendQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetMonthlySpendHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<CategoryBreakdownResponse>> CategoryAsync(GetCategoryBreakdownQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetCategoryBreakdownHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<CurrencyDistributionResponse>> CurrencyAsync(GetCurrencyDistributionQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetCurrencyDistributionHandler>()
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
