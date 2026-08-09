using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories.DeleteUserCategory;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.1.5 — soft-delete user category; conflict if active subs.</summary>
public class DeleteUserCategoryHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Delete_own_unused_category_soft_deletes()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var id = await harness.SeedCategoryAsync(userId, "Temp");

        var result = await harness.DeleteAsync(id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        Assert.False(await db.UserCategories.AnyAsync(c => c.Id == id));
        var soft = await db.UserCategories.IgnoreQueryFilters().SingleAsync(c => c.Id == id);
        Assert.NotNull(soft.DeletedAt);
    }

    [Fact]
    public async Task Delete_blocked_when_active_subscription_references()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var catId = await harness.SeedCategoryAsync(userId, "In Use");

        var created = await harness.CreateSubAsync(new CreateSubscriptionCommand(
            "Gym", 50m, "TRY", "monthly", 1, Today.AddDays(5), UserCategoryId: catId));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);

        var result = await harness.DeleteAsync(catId);
        Assert.Equal(DomainErrors.UserCategoryErrors.HasActiveSubscriptions.Code, result.Error.Code);
    }

    [Fact]
    public async Task Delete_allowed_when_only_archived_subscription_references()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var catId = await harness.SeedCategoryAsync(userId, "Old Use");

        var created = await harness.CreateSubAsync(new CreateSubscriptionCommand(
            "Old Gym", 50m, "TRY", "monthly", 1, Today.AddDays(5), UserCategoryId: catId));
        Assert.True(created.IsSuccess);
        await harness.ArchiveSubAsync(created.Value.Id);

        var result = await harness.DeleteAsync(catId);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
    }

    [Fact]
    public async Task Delete_foreign_returns_access_denied()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");
        var id = await harness.SeedCategoryAsync(owner, "Private");

        harness.SetUser(other);
        var result = await harness.DeleteAsync(id);
        Assert.Equal(DomainErrors.UserCategoryErrors.AccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Delete_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));
        var result = await harness.DeleteAsync(Guid.CreateVersion7());
        Assert.Equal(DomainErrors.UserCategoryErrors.NotFound.Code, result.Error.Code);
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
            services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<DeleteUserCategoryHandler>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<SubifyDbContext>().Database.EnsureCreatedAsync();
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

            return user.Id;
        }

        public async Task<Guid> SeedCategoryAsync(Guid userId, string name)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name);
            db.UserCategories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Result<CreateSubscriptionResponse>> CreateSubAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task ArchiveSubAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.IgnoreQueryFilters().SingleAsync(s => s.Id == id);
            sub.Archive();
            await db.SaveChangesAsync();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<DeleteUserCategoryHandler>()
                .Handle(new DeleteUserCategoryCommand(id), CancellationToken.None);
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
