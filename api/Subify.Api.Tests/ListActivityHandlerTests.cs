using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Activity;
using Subify.Application.Features.Activity.ListActivity;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.4.2 — list own activity with pagination and entityType filter.</summary>
public class ListActivityHandlerTests
{
    [Fact]
    public async Task Lists_only_own_logs_newest_first()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");

        await harness.LogAsync(other, ActivityLogConstants.EntityTypes.Profile, "profile.updated", "other");
        await harness.LogAsync(owner, ActivityLogConstants.EntityTypes.Subscription, "subscription.created", "first");
        await Task.Delay(5); // ensure CreatedAt ordering
        await harness.LogAsync(owner, ActivityLogConstants.EntityTypes.Profile, "profile.updated", "second");

        harness.SetUser(owner);
        var result = await harness.HandleAsync(new ListActivityQuery(Page: 1, PageSize: 10));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(2, result.Value.Pagination.TotalItems);
        Assert.Equal(2, result.Value.Data.Count);
        Assert.Equal("second", result.Value.Data[0].Description);
        Assert.Equal("first", result.Value.Data[1].Description);
        Assert.DoesNotContain(result.Value.Data, a => a.Description == "other");
    }

    [Fact]
    public async Task Filters_by_entity_type_and_paginates()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        await harness.LogAsync(userId, ActivityLogConstants.EntityTypes.Subscription, "subscription.created", "s1");
        await harness.LogAsync(userId, ActivityLogConstants.EntityTypes.Subscription, "subscription.updated", "s2");
        await harness.LogAsync(userId, ActivityLogConstants.EntityTypes.Profile, "profile.updated", "p1");

        var filtered = await harness.HandleAsync(new ListActivityQuery(
            EntityType: "subscription",
            Page: 1,
            PageSize: 10));
        Assert.Equal(2, filtered.Value.Pagination.TotalItems);
        Assert.All(filtered.Value.Data, a =>
            Assert.Equal(ActivityLogConstants.EntityTypes.Subscription, a.EntityType));

        var page1 = await harness.HandleAsync(new ListActivityQuery(Page: 1, PageSize: 2));
        Assert.Equal(3, page1.Value.Pagination.TotalItems);
        Assert.Equal(2, page1.Value.Pagination.TotalPages);
        Assert.Equal(2, page1.Value.Data.Count);

        var page2 = await harness.HandleAsync(new ListActivityQuery(Page: 2, PageSize: 2));
        Assert.Single(page2.Value.Data);
    }

    [Fact]
    public async Task Unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync(new ListActivityQuery());
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
            services.AddScoped<ListActivityHandler>();

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

        public async Task LogAsync(Guid userId, string entityType, string action, string description)
        {
            await using var scope = _provider.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<IActivityLogger>();
            await logger.LogAndSaveAsync(
                userId,
                entityType,
                action,
                description,
                entityId: userId);
        }

        public async Task<Result<ListActivityResponse>> HandleAsync(ListActivityQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListActivityHandler>()
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
