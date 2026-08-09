using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Providers;
using Subify.Application.Features.Providers.Admin.CreateAdminProvider;
using Subify.Application.Features.Providers.Admin.DeleteAdminProvider;
using Subify.Application.Features.Providers.Admin.UpdateAdminProvider;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.2.3 — SuperAdmin provider CRUD.</summary>
public class AdminProviderHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Create_as_super_admin_persists()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin));

        var result = await harness.CreateAsync(new CreateAdminProviderCommand(
            Name: "Netflix",
            Slug: "netflix",
            Currency: "TRY",
            BillingCycle: "monthly",
            Region: "TR",
            Price: 149.99m));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("netflix", result.Value.Slug);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Create_rejects_duplicate_slug_and_non_admin()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin));
        Assert.True((await harness.CreateAsync(new CreateAdminProviderCommand(
            "A", "dup-slug", "USD", "monthly", "US"))).IsSuccess);

        var dup = await harness.CreateAsync(new CreateAdminProviderCommand(
            "B", "dup-slug", "USD", "monthly", "US"));
        Assert.Equal(DomainErrors.ProviderErrors.DuplicateSlug.Code, dup.Error.Code);

        harness.SetUser(await harness.SeedUserAsync("user@subify.local", AppRoles.User));
        var denied = await harness.CreateAsync(new CreateAdminProviderCommand(
            "C", "other", "USD", "monthly", "US"));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, denied.Error.Code);
    }

    [Fact]
    public async Task Update_and_delete_with_subscription_guard()
    {
        await using var harness = await Harness.CreateAsync();
        var adminId = await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(adminId);

        var created = await harness.CreateAsync(new CreateAdminProviderCommand(
            "Tool", "tool-app", "USD", "yearly", "GLOBAL", Price: 20m));
        Assert.True(created.IsSuccess);

        var updated = await harness.UpdateAsync(new UpdateAdminProviderCommand(
            created.Value.Id,
            "Tool Pro",
            "tool-app",
            "USD",
            "yearly",
            "GLOBAL",
            Price: 25m,
            IsActive: true));
        Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Code : null);
        Assert.Equal("Tool Pro", updated.Value.Name);
        Assert.Equal(25m, updated.Value.Price);

        // Active sub blocks delete
        var userId = await harness.SeedUserAsync("u@subify.local", AppRoles.User);
        harness.SetUser(userId);
        var sub = await harness.CreateSubAsync(new CreateSubscriptionCommand(
            "Tool Sub", 20m, "USD", "monthly", 1, Today.AddDays(5),
            ProviderId: created.Value.Id));
        Assert.True(sub.IsSuccess, sub.IsFailure ? sub.Error.Code : null);

        harness.SetUser(adminId);
        var blocked = await harness.DeleteAsync(created.Value.Id);
        Assert.Equal(DomainErrors.ProviderErrors.HasActiveSubscriptions.Code, blocked.Error.Code);

        await harness.ArchiveSubAsync(sub.Value.Id);
        var deleted = await harness.DeleteAsync(created.Value.Id);
        Assert.True(deleted.IsSuccess, deleted.IsFailure ? deleted.Error.Code : null);
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
            services.AddScoped<CreateAdminProviderHandler>();
            services.AddScoped<UpdateAdminProviderHandler>();
            services.AddScoped<DeleteAdminProviderHandler>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<CreateSubscriptionHandler>();

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

        public async Task<Result<ProviderResponse>> CreateAsync(CreateAdminProviderCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateAdminProviderHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ProviderResponse>> UpdateAsync(UpdateAdminProviderCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateAdminProviderHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<DeleteAdminProviderHandler>()
                .Handle(new DeleteAdminProviderCommand(id), CancellationToken.None);
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
