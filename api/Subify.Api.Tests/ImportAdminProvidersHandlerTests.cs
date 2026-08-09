using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Providers.Admin.ImportAdminProviders;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>16.6.3 — SuperAdmin bulk provider catalog import.</summary>
public class ImportAdminProvidersHandlerTests
{
    [Fact]
    public async Task Import_creates_missing_and_skips_existing()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin));
        await harness.SeedProviderAsync("Netflix", "netflix");

        var result = await harness.ImportAsync(new ImportAdminProvidersCommand(
        [
            new ImportProviderItem("Netflix", "netflix", "TRY", "monthly", "TR", 100m),
            new ImportProviderItem("Spotify", "spotify", "TRY", "monthly", "TR", 50m),
        ]));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(0, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(0, result.Value.Failed);
    }

    [Fact]
    public async Task Import_update_existing_overwrites_price()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin));
        await harness.SeedProviderAsync("Spotify", "spotify", price: 10m);

        var result = await harness.ImportAsync(new ImportAdminProvidersCommand(
        [
            new ImportProviderItem("Spotify", "spotify", "TRY", "monthly", "TR", 99m),
        ], UpdateExisting: true));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(1, result.Value.Updated);

        var price = await harness.GetPriceAsync("spotify");
        Assert.Equal(99m, price);
    }

    [Fact]
    public async Task Import_rejects_non_super_admin()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("user@subify.local", AppRoles.User));

        var result = await harness.ImportAsync(new ImportAdminProvidersCommand(
        [
            new ImportProviderItem("X", "x-app", "TRY", "monthly", "TR", 1m),
        ]));

        Assert.True(result.IsFailure);
        Assert.Equal(Domain.Errors.DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
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
            services.AddScoped<ImportAdminProvidersHandler>();

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
            using var scope = _provider.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = users.FindByIdAsync(userId.ToString()).GetAwaiter().GetResult()
                       ?? throw new InvalidOperationException("missing user");
            var roles = users.GetRolesAsync(user).GetAwaiter().GetResult();
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = roles.ToList();
        }

        public async Task<Guid> SeedUserAsync(string email, string role)
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

            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task SeedProviderAsync(string name, string slug, decimal price = 10m)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            db.Providers.Add(Provider.CreateCatalog(name, slug, "TRY", price, BillingCycle.Monthly, "TR"));
            await db.SaveChangesAsync();
        }

        public async Task<decimal?> GetPriceAsync(string slug)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.Providers.Where(p => p.Slug == slug).Select(p => p.Price).SingleAsync();
        }

        public async Task<Result<ImportAdminProvidersResponse>> ImportAsync(ImportAdminProvidersCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ImportAdminProvidersHandler>()
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
