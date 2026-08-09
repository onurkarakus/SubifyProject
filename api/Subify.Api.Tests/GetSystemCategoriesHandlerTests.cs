using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories;
using Subify.Application.Features.Categories.GetSystemCategories;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.1.1 / 5.1.6 — system categories with localized names.</summary>
public class GetSystemCategoriesHandlerTests
{
    [Fact]
    public async Task Lists_active_categories_with_tr_names_from_accept_language()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", locale: "en");
        harness.SetUser(userId, locale: "en");

        var result = await harness.HandleAsync(new GetSystemCategoriesQuery(AcceptLanguage: "tr-TR,tr;q=0.9"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);

        Assert.Equal(3, result.Value.Data.Count);
        Assert.Equal(["streaming", "music", "other"], result.Value.Data.Select(c => c.Slug).ToArray());
        Assert.Equal("Video Akış", result.Value.Data[0].Name);
        Assert.Equal("Müzik", result.Value.Data[1].Name);
        Assert.Equal("Diğer", result.Value.Data[2].Name);
        Assert.Equal(1, result.Value.Data[0].SortOrder);
    }

    [Fact]
    public async Task Explicit_locale_query_overrides_header()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", locale: "tr");
        harness.SetUser(userId, locale: "tr");

        var result = await harness.HandleAsync(new GetSystemCategoriesQuery(
            AcceptLanguage: "tr",
            ExplicitLocale: "en"));

        Assert.Equal("Streaming", result.Value.Data[0].Name);
        Assert.Equal("Music", result.Value.Data[1].Name);
    }

    [Fact]
    public async Task Falls_back_to_user_locale_then_slug()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local", locale: "en");
        harness.SetUser(userId, locale: "en");

        // No Accept-Language / explicit → user en
        var fromUser = await harness.HandleAsync(new GetSystemCategoriesQuery());
        Assert.Equal("Streaming", fromUser.Value.Data[0].Name);

        // Unknown slug without resource → slug as name
        await harness.SeedCategoryAsync("custom-cat", "icon", "#000", 50, withResource: false);
        var list = await harness.HandleAsync(new GetSystemCategoriesQuery(ExplicitLocale: "en"));
        var custom = list.Value.Data.Single(c => c.Slug == "custom-cat");
        Assert.Equal("custom-cat", custom.Name);
    }

    [Fact]
    public async Task Excludes_inactive_categories()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var inactiveId = await harness.SeedCategoryAsync("dead", null, null, 99, active: false);
        var result = await harness.HandleAsync(new GetSystemCategoriesQuery(ExplicitLocale: "en"));
        Assert.DoesNotContain(result.Value.Data, c => c.Id == inactiveId || c.Slug == "dead");
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
            services.AddScoped<ICategoryNameLookup, CategoryNameLookup>();
            services.AddScoped<GetSystemCategoriesHandler>();

            var provider = services.BuildServiceProvider();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();

                // seed 3 system categories + TR/EN names
                await SeedCatalogAsync(db);
            }

            return new Harness(connection, provider);
        }

        private static async Task SeedCatalogAsync(SubifyDbContext db)
        {
            var streaming = Category.CreateSystem("streaming", "play-circle", "#E50914", 1);
            var music = Category.CreateSystem("music", "music-note", "#1DB954", 2);
            var other = Category.CreateSystem("other", "more", "#999", 3);
            db.Categories.AddRange(streaming, music, other);

            db.Resources.AddRange(
                Resource.Create(SystemResources.Pages.Category, "streaming", "tr", "Video Akış"),
                Resource.Create(SystemResources.Pages.Category, "streaming", "en", "Streaming"),
                Resource.Create(SystemResources.Pages.Category, "music", "tr", "Müzik"),
                Resource.Create(SystemResources.Pages.Category, "music", "en", "Music"),
                Resource.Create(SystemResources.Pages.Category, "other", "tr", "Diğer"),
                Resource.Create(SystemResources.Pages.Category, "other", "en", "Other"));

            await db.SaveChangesAsync();
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
            user.Locale = locale;
            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, "Password1");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

            return user.Id;
        }

        public async Task<Guid> SeedCategoryAsync(
            string slug,
            string? icon,
            string? color,
            int sortOrder,
            bool active = true,
            bool withResource = true)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = Category.CreateSystem(slug, icon, color, sortOrder);
            if (!active)
            {
                c.Deactivate();
            }

            db.Categories.Add(c);
            if (withResource)
            {
                db.Resources.Add(Resource.Create(SystemResources.Pages.Category, slug, "en", slug));
            }

            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Result<ListCategoriesResponse>> HandleAsync(GetSystemCategoriesQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetSystemCategoriesHandler>()
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
