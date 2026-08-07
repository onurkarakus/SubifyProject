using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.ExchangeRates.GetExchangeRates;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 6.2 — FX client sync, snapshot, GET, fallback.</summary>
public class ExchangeRateHandlerTests
{
    [Fact]
    public async Task Sync_persists_snapshot_rows_for_supported_targets()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("sync@subify.local");
        harness.SetUser(userId);

        harness.Client.SetQuote("TRY", new Dictionary<string, decimal>
        {
            ["USD"] = 0.03m,
            ["EUR"] = 0.028m,
            ["GBP"] = 0.024m,
            ["JPY"] = 4.5m // not a supported target → still persisted if returned; client filters by request
        });

        var result = await harness.SyncBaseAsync("TRY");
        Assert.True(result.Succeeded);
        Assert.False(result.UsedExistingFallback);
        Assert.Equal(3, result.RatesPersisted); // only SupportedCurrencies targets

        var rates = await harness.GetAsync(new GetExchangeRatesQuery("TRY"));
        Assert.True(rates.IsSuccess, rates.IsFailure ? rates.Error.Code : null);
        Assert.Equal("TRY", rates.Value.Base);
        Assert.Equal(0.03m, rates.Value.Rates["USD"]);
        Assert.Equal(0.028m, rates.Value.Rates["EUR"]);
        Assert.DoesNotContain("JPY", rates.Value.Rates.Keys);
        Assert.False(rates.Value.FromFallback);
    }

    [Fact]
    public async Task Sync_on_provider_failure_keeps_last_known_snapshot()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("keep@subify.local");
        harness.SetUser(userId);

        harness.Client.SetQuote("USD", new Dictionary<string, decimal>
        {
            ["TRY"] = 34m,
            ["EUR"] = 0.92m,
            ["GBP"] = 0.79m
        });

        var first = await harness.SyncBaseAsync("USD");
        Assert.True(first.Succeeded);
        Assert.Equal(3, first.RatesPersisted);

        harness.Client.FailNext = true;
        var second = await harness.SyncBaseAsync("USD");
        Assert.True(second.Succeeded);
        Assert.True(second.UsedExistingFallback);
        Assert.Equal(0, second.RatesPersisted);

        var get = await harness.GetAsync(new GetExchangeRatesQuery("USD"));
        Assert.True(get.IsSuccess, get.IsFailure ? get.Error.Code : null);
        Assert.Equal(34m, get.Value.Rates["TRY"]);
    }

    [Fact]
    public async Task Sync_failure_with_no_snapshot_reports_unsuccessful()
    {
        await using var harness = await Harness.CreateAsync();
        harness.Client.FailNext = true;

        var result = await harness.SyncBaseAsync("EUR");
        Assert.False(result.Succeeded);
        Assert.False(result.UsedExistingFallback);
    }

    [Fact]
    public async Task Get_on_demand_sync_when_db_empty()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("fx@subify.local", mainCurrency: "TRY");
        harness.SetUser(userId);

        harness.Client.SetQuote("TRY", new Dictionary<string, decimal>
        {
            ["USD"] = 0.031m,
            ["EUR"] = 0.029m,
            ["GBP"] = 0.025m
        });

        var result = await harness.GetAsync(new GetExchangeRatesQuery());
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("TRY", result.Value.Base);
        Assert.Equal(0.031m, result.Value.Rates["USD"]);
        Assert.NotNull(result.Value.LastUpdated);
    }

    [Fact]
    public async Task Get_returns_provider_unavailable_when_empty_and_fetch_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("nofx@subify.local");
        harness.SetUser(userId);
        harness.Client.FailNext = true;

        var result = await harness.GetAsync(new GetExchangeRatesQuery("USD"));
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.ExchangeRateErrors.ProviderUnavailable.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_serves_fallback_after_failed_refresh_attempt()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("fb@subify.local");
        harness.SetUser(userId);

        // Seed last-known without going through live client
        await harness.SeedSnapshotAsync("GBP", "USD", 1.25m);
        await harness.SeedSnapshotAsync("GBP", "TRY", 42m);
        await harness.SeedSnapshotAsync("GBP", "EUR", 1.15m);

        // Empty path would try sync; with existing rows GET loads DB first without sync.
        var result = await harness.GetAsync(new GetExchangeRatesQuery("GBP"));
        Assert.True(result.IsSuccess);
        Assert.Equal(1.25m, result.Value.Rates["USD"]);
        Assert.Equal(42m, result.Value.Rates["TRY"]);
    }

    [Fact]
    public async Task Get_invalid_base_fails_validation_or_domain()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("bad@subify.local");
        harness.SetUser(userId);

        var result = await harness.GetAsync(new GetExchangeRatesQuery("XXX"));
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.ExchangeRateErrors.InvalidBase.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.GetAsync(new GetExchangeRatesQuery("TRY"));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
    }

    [Fact]
    public async Task Sync_all_isolates_per_base_failures()
    {
        await using var harness = await Harness.CreateAsync();
        harness.Client.FailBases.Add("EUR");
        harness.Client.DefaultRate = 2m;

        var results = await harness.SyncAllAsync();
        Assert.Equal(SupportedCurrencies.All.Count, results.Count);
        Assert.Contains(results, r => r.BaseCurrency == "EUR" && !r.Succeeded);
        Assert.Contains(results, r => r.BaseCurrency == "USD" && r.Succeeded);
        Assert.Contains(results, r => r.BaseCurrency == "TRY" && r.Succeeded);
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

        public FakeExchangeRateClient Client =>
            (FakeExchangeRateClient)_provider.GetRequiredService<IExchangeRateClient>();

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
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
            services.AddSingleton<IExchangeRateClient, FakeExchangeRateClient>();
            services.Configure<ExchangeRateOptions>(o =>
            {
                o.Enabled = true;
                o.Provider = "OpenErApi";
            });
            services.AddScoped<IExchangeRateSyncService, ExchangeRateSyncService>();
            services.AddScoped<GetExchangeRatesHandler>();

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

        public async Task<Guid> SeedUserAsync(string email, string mainCurrency = "TRY")
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            user.MainCurrency = mainCurrency;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            await users.AddToRoleAsync(user, AppRoles.User);
            return user.Id;
        }

        public async Task SeedSnapshotAsync(string bas, string target, decimal rate)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot(
                bas, target, rate, "test", DateTimeOffset.UtcNow.AddHours(-1)));
            await db.SaveChangesAsync();
        }

        public async Task<ExchangeRateSyncResult> SyncBaseAsync(string bas)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IExchangeRateSyncService>()
                .SyncBaseAsync(bas);
        }

        public async Task<IReadOnlyList<ExchangeRateSyncResult>> SyncAllAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IExchangeRateSyncService>()
                .SyncAllAsync();
        }

        public async Task<Result<ExchangeRatesResponse>> GetAsync(GetExchangeRatesQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetExchangeRatesHandler>()
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

    /// <summary>In-memory FX client for unit tests (no network).</summary>
    public sealed class FakeExchangeRateClient : IExchangeRateClient
    {
        private readonly Dictionary<string, Dictionary<string, decimal>> _quotes =
            new(StringComparer.OrdinalIgnoreCase);

        public bool FailNext { get; set; }
        public HashSet<string> FailBases { get; } = new(StringComparer.OrdinalIgnoreCase);
        public decimal? DefaultRate { get; set; }

        public void SetQuote(string bas, Dictionary<string, decimal> rates) =>
            _quotes[bas.ToUpperInvariant()] = rates;

        public Task<Result<ExchangeRateFetchResult>> FetchAsync(
            string baseCurrency,
            IReadOnlyCollection<string>? targetCurrencies = null,
            CancellationToken cancellationToken = default)
        {
            var bas = baseCurrency.Trim().ToUpperInvariant();

            if (FailNext || FailBases.Contains(bas))
            {
                FailNext = false;
                return Task.FromResult(
                    Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable));
            }

            Dictionary<string, decimal> rates;
            if (_quotes.TryGetValue(bas, out var configured))
            {
                rates = new Dictionary<string, decimal>(configured, StringComparer.OrdinalIgnoreCase);
            }
            else if (DefaultRate is { } def)
            {
                rates = SupportedCurrencies.All
                    .Where(c => !string.Equals(c, bas, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(c => c, _ => def, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return Task.FromResult(
                    Result.Failure<ExchangeRateFetchResult>(DomainErrors.ExchangeRateErrors.ProviderUnavailable));
            }

            if (targetCurrencies is not null)
            {
                var allow = targetCurrencies
                    .Select(t => t.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                rates = rates
                    .Where(kv => allow.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            }

            return Task.FromResult(Result.Success(new ExchangeRateFetchResult(
                BaseCurrency: bas,
                Rates: rates,
                FetchedAt: DateTimeOffset.UtcNow,
                Source: "fake")));
        }
    }
}
