using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.GetSubscriptionById;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.3 — GetSubscriptionById ownership / 404 / 403.</summary>
public class GetSubscriptionByIdHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Get_own_subscription_returns_detail()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("owner@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5)));
        Assert.True(created.IsSuccess);

        var result = await harness.GetAsync(created.Value.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("Netflix", result.Value.Name);
        Assert.Equal(100m, result.Value.Price);
        Assert.Equal(userId, await harness.GetOwnerIdAsync(created.Value.Id));
    }

    [Fact]
    public async Task Get_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.GetAsync(Guid.CreateVersion7());
        Assert.Equal(DomainErrors.Subscription.SubscriptionNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_foreign_subscription_returns_access_denied()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        harness.SetUser(owner);
        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Private", 50m, "TRY", "monthly", 1, Today.AddDays(3)));
        Assert.True(created.IsSuccess);

        harness.SetUser(other);
        var result = await harness.GetAsync(created.Value.Id);
        Assert.Equal(DomainErrors.Subscription.SubscriptionAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Get_own_archived_still_returns()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("arch@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Archived One", 10m, "USD", "yearly", 1, Today.AddDays(20)));
        Assert.True(created.IsSuccess);

        await harness.ArchiveAsync(created.Value.Id);

        var result = await harness.GetAsync(created.Value.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.True(result.Value.Archived);
        Assert.Equal("Archived One", result.Value.Name);
    }

    [Fact]
    public async Task Get_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.GetAsync(Guid.CreateVersion7());
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
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<GetSubscriptionByIdHandler>();

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

        public async Task<Result<CreateSubscriptionResponse>> CreateAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<SubscriptionResponse>> GetAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetSubscriptionByIdHandler>()
                .Handle(new GetSubscriptionByIdQuery(id), CancellationToken.None);
        }

        public async Task ArchiveAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var sub = await db.Subscriptions.IgnoreQueryFilters().SingleAsync(s => s.Id == id);
            sub.Archive();
            await db.SaveChangesAsync();
        }

        public async Task<Guid> GetOwnerIdAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.Subscriptions.IgnoreQueryFilters()
                .Where(s => s.Id == id)
                .Select(s => s.UserId)
                .SingleAsync();
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
