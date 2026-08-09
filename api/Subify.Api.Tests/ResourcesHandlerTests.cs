using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Resources;
using Subify.Application.Features.Resources.Admin.CreateAdminResource;
using Subify.Application.Features.Resources.Admin.DeleteAdminResource;
using Subify.Application.Features.Resources.Admin.ListAdminResources;
using Subify.Application.Features.Resources.Admin.UpdateAdminResource;
using Subify.Application.Features.Resources.GetResources;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 6.3 — resources delta sync, memory cache, admin CRUD.</summary>
public class ResourcesHandlerTests
{
    [Fact]
    public async Task Get_full_pack_filters_by_lang()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", AppRoles.User, locale: "en");
        harness.SetUser(userId, AppRoles.User, locale: "en");

        await harness.SeedResourceAsync("Dashboard", "title", "tr", "Ana Sayfa");
        await harness.SeedResourceAsync("Dashboard", "title", "en", "Home");
        await harness.SeedResourceAsync("Common", "save", "tr", "Kaydet");
        await harness.SeedResourceAsync("Common", "save", "en", "Save");

        var tr = await harness.GetAsync(new GetResourcesQuery(Lang: "tr"));
        Assert.True(tr.IsSuccess, tr.IsFailure ? tr.Error.Code : null);
        Assert.False(tr.Value.NotModified);
        Assert.Equal(2, tr.Value.Data.Count);
        Assert.Contains(tr.Value.Data, r => r is { PageName: "Dashboard", Name: "title", Value: "Ana Sayfa" });
        Assert.DoesNotContain(tr.Value.Data, r => r.Value == "Home");
        Assert.NotNull(tr.Value.LastUpdated);

        var en = await harness.GetAsync(new GetResourcesQuery(Lang: "en"));
        Assert.Equal(2, en.Value.Data.Count);
        Assert.Contains(en.Value.Data, r => r.Value == "Home");
    }

    [Fact]
    public async Task Get_uses_memory_cache_for_full_pack()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("c@subify.local"), AppRoles.User);
        await harness.SeedResourceAsync("Common", "ok", "tr", "Tamam");

        var first = await harness.GetAsync(new GetResourcesQuery(Lang: "tr"));
        Assert.True(first.IsSuccess);
        Assert.Single(first.Value.Data);

        // Mutate DB under the cache without going through admin (simulate stale write)
        await harness.OverwriteValueDirectAsync("Common", "ok", "tr", "CHANGED");

        var cached = await harness.GetAsync(new GetResourcesQuery(Lang: "tr"));
        Assert.Equal("Tamam", cached.Value.Data[0].Value); // still cached
    }

    [Fact]
    public async Task Get_delta_returns_only_changed_and_304_when_none()
    {
        await using var harness = await Harness.CreateAsync();
        var adminId = await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(adminId, AppRoles.SuperAdmin);

        await harness.SeedResourceAsync("Common", "a", "tr", "A");
        var before = DateTimeOffset.UtcNow;
        await Task.Delay(20);

        var created = await harness.CreateAdminAsync(new CreateAdminResourceCommand(
            "Common", "b", "tr", "B"));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);

        var delta = await harness.GetAsync(new GetResourcesQuery(Lang: "tr", Since: before));
        Assert.True(delta.IsSuccess);
        Assert.False(delta.Value.NotModified);
        Assert.Contains(delta.Value.Data, r => r.Name == "b");
        // "a" was created before `before` → not in delta (unless clock skew). Tolerate only b.
        Assert.DoesNotContain(delta.Value.Data, r => r.Name == "a");

        var afterAll = DateTimeOffset.UtcNow.AddSeconds(1);
        var none = await harness.GetAsync(new GetResourcesQuery(Lang: "tr", Since: afterAll));
        Assert.True(none.IsSuccess);
        Assert.True(none.Value.NotModified);
        Assert.Empty(none.Value.Data);
    }

    [Fact]
    public async Task Admin_update_invalidates_cache_and_appears_in_delta()
    {
        await using var harness = await Harness.CreateAsync();
        var adminId = await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(adminId, AppRoles.SuperAdmin);

        var created = await harness.CreateAdminAsync(new CreateAdminResourceCommand(
            "Dashboard", "title", "en", "Home"));
        Assert.True(created.IsSuccess);

        // Warm cache
        var pack = await harness.GetAsync(new GetResourcesQuery(Lang: "en"));
        Assert.Equal("Home", pack.Value.Data.Single().Value);

        var since = DateTimeOffset.UtcNow;
        await Task.Delay(20);

        var updated = await harness.UpdateAdminAsync(new UpdateAdminResourceCommand(
            created.Value.Id,
            "Dashboard",
            "title",
            "en",
            "Home Updated"));
        Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Code : null);

        var after = await harness.GetAsync(new GetResourcesQuery(Lang: "en"));
        Assert.Equal("Home Updated", after.Value.Data.Single().Value);

        var delta = await harness.GetAsync(new GetResourcesQuery(Lang: "en", Since: since));
        Assert.False(delta.Value.NotModified);
        Assert.Contains(delta.Value.Data, r => r.Value == "Home Updated");
    }

    [Fact]
    public async Task Admin_create_conflict_and_delete()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var a = await harness.CreateAdminAsync(new CreateAdminResourceCommand(
            "Error", "x", "tr", "Hata"));
        Assert.True(a.IsSuccess);

        var dup = await harness.CreateAdminAsync(new CreateAdminResourceCommand(
            "Error", "x", "tr", "Again"));
        Assert.Equal(DomainErrors.ResourceErrors.ResourceConflict.Code, dup.Error.Code);

        var deleted = await harness.DeleteAdminAsync(a.Value.Id);
        Assert.True(deleted.IsSuccess);

        var missing = await harness.DeleteAdminAsync(a.Value.Id);
        Assert.Equal(DomainErrors.ResourceErrors.ResourceNotFound.Code, missing.Error.Code);
    }

    [Fact]
    public async Task Admin_list_filters_and_non_super_admin_denied()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);
        await harness.CreateAdminAsync(new CreateAdminResourceCommand("Common", "save", "tr", "Kaydet"));
        await harness.CreateAdminAsync(new CreateAdminResourceCommand("Common", "save", "en", "Save"));
        await harness.CreateAdminAsync(new CreateAdminResourceCommand("Dashboard", "title", "tr", "Ana"));

        var tr = await harness.ListAdminAsync(new ListAdminResourcesQuery(Lang: "tr"));
        Assert.Equal(2, tr.Value.Data.Count);

        var page = await harness.ListAdminAsync(new ListAdminResourcesQuery(PageName: "Dashboard"));
        Assert.Single(page.Value.Data);

        harness.SetUser(await harness.SeedUserAsync("user@subify.local", AppRoles.User), AppRoles.User);
        var denied = await harness.CreateAdminAsync(new CreateAdminResourceCommand(
            "Common", "nope", "tr", "x"));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, denied.Error.Code);
    }

    [Fact]
    public async Task Get_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.GetAsync(new GetResourcesQuery(Lang: "tr"));
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
            services.AddScoped<GetResourcesHandler>();
            services.AddScoped<ListAdminResourcesHandler>();
            services.AddScoped<CreateAdminResourceHandler>();
            services.AddScoped<UpdateAdminResourceHandler>();
            services.AddScoped<DeleteAdminResourceHandler>();

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

        public void SetUser(Guid userId, string role, string? locale = null)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Locale = locale;
            fake.Roles = [role];
        }

        public async Task<Guid> SeedUserAsync(string email, string role = AppRoles.User, string locale = "tr")
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

            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task SeedResourceAsync(string page, string name, string lang, string value)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            db.Resources.Add(Resource.Create(page, name, lang, value));
            await db.SaveChangesAsync();
        }

        public async Task OverwriteValueDirectAsync(string page, string name, string lang, string value)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var row = await db.Resources.SingleAsync(r =>
                r.PageName == page && r.Name == name && r.LanguageCode == lang);
            // DB write without ResourceCache.Invalidate — proves full pack is memory-cached.
            row.Update(row.PageName, row.Name, row.LanguageCode, value);
            await db.SaveChangesAsync();
        }

        public async Task<Result<ListResourcesResponse>> GetAsync(GetResourcesQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetResourcesHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<AdminResourceResponse>> CreateAdminAsync(CreateAdminResourceCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateAdminResourceHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<AdminResourceResponse>> UpdateAdminAsync(UpdateAdminResourceCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateAdminResourceHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result> DeleteAdminAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<DeleteAdminResourceHandler>()
                .Handle(new DeleteAdminResourceCommand(id), CancellationToken.None);
        }

        public async Task<Result<ListAdminResourcesResponse>> ListAdminAsync(ListAdminResourcesQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListAdminResourcesHandler>()
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
