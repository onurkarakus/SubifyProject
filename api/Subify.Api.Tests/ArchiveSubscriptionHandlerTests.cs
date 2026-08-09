using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Application.Features.Subscriptions.ArchiveSubscription;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 4.1.7 — ArchiveSubscription soft-delete + activity.</summary>
public class ArchiveSubscriptionHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Archive_own_sets_archived_and_writes_activity()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("owner@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Spotify", 59.99m, "TRY", "monthly", 1, Today.AddDays(7)));
        Assert.True(created.IsSuccess);

        var archived = await harness.ArchiveAsync(created.Value.Id);
        Assert.True(archived.IsSuccess, archived.IsFailure ? archived.Error.Code : null);
        Assert.True(archived.Value.Archived);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var row = await db.Subscriptions.IgnoreQueryFilters()
            .SingleAsync(s => s.Id == created.Value.Id);
        Assert.True(row.Archived);
        Assert.NotNull(row.DeletedAt);

        // Soft-delete filter hides from default query
        Assert.False(await db.Subscriptions.AnyAsync(s => s.Id == created.Value.Id));

        var log = await db.ActivityLogs.SingleAsync(a =>
            a.EntityId == created.Value.Id
            && a.Action == ActivityLogConstants.Actions.SubscriptionArchived);
        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("\"archived\":false", log.OldValues, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"archived\":true", log.NewValues, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Archive_twice_is_idempotent_without_duplicate_activity()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Once", 10m, "TRY", "monthly", 1, Today.AddDays(3)));

        Assert.True((await harness.ArchiveAsync(created.Value.Id)).IsSuccess);
        Assert.True((await harness.ArchiveAsync(created.Value.Id)).IsSuccess);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var count = await db.ActivityLogs.CountAsync(a =>
            a.EntityId == created.Value.Id
            && a.Action == ActivityLogConstants.Actions.SubscriptionArchived);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Archive_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.ArchiveAsync(Guid.CreateVersion7());
        Assert.Equal(DomainErrors.Subscription.SubscriptionNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Archive_foreign_returns_access_denied()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        harness.SetUser(owner);
        var created = await harness.CreateAsync(new CreateSubscriptionCommand(
            "Private", 20m, "TRY", "monthly", 1, Today.AddDays(5)));

        harness.SetUser(other);
        var result = await harness.ArchiveAsync(created.Value.Id);
        Assert.Equal(DomainErrors.Subscription.SubscriptionAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Archive_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.ArchiveAsync(Guid.CreateVersion7());
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
            services.AddScoped<ArchiveSubscriptionHandler>();

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

        public async Task<Result<SubscriptionResponse>> ArchiveAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ArchiveSubscriptionHandler>()
                .Handle(new ArchiveSubscriptionCommand(id), CancellationToken.None);
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
