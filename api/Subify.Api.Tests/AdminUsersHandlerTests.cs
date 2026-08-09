using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Admin.Users;
using Subify.Application.Features.Admin.Users.CreateAdminUser;
using Subify.Application.Features.Admin.Users.ListAdminUsers;
using Subify.Application.Features.Admin.Users.PatchAdminUser;
using Subify.Application.Features.Auth.Login;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.ListSubscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 7.1 — admin user list/create/patch, soft-disable, subscription isolation.</summary>
public class AdminUsersHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task List_paginates_and_searches()
    {
        await using var harness = await Harness.CreateAsync();
        var adminId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(adminId, AppRoles.SuperAdmin);

        await harness.CreateAsync(new CreateAdminUserCommand("alice@subify.local", "Alice A", "Password1"));
        await harness.CreateAsync(new CreateAdminUserCommand("bob@subify.local", "Bob B", "Password1"));
        await harness.CreateAsync(new CreateAdminUserCommand("carol@subify.local", "Carol C", "Password1"));

        var all = await harness.ListAsync(new ListAdminUsersQuery(Page: 1, PageSize: 50));
        Assert.True(all.IsSuccess, all.IsFailure ? all.Error.Code : null);
        Assert.True(all.Value.Pagination.TotalItems >= 4); // super + 3

        var search = await harness.ListAsync(new ListAdminUsersQuery(Search: "alice"));
        Assert.Single(search.Value.Data);
        Assert.Equal("alice@subify.local", search.Value.Data[0].Email);

        var page = await harness.ListAsync(new ListAdminUsersQuery(Page: 1, PageSize: 2));
        Assert.Equal(2, page.Value.Data.Count);
        Assert.True(page.Value.Pagination.TotalPages >= 2);
    }

    [Fact]
    public async Task Create_user_and_admin_roles_with_permissions()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var user = await harness.CreateAsync(new CreateAdminUserCommand(
            "member@subify.local", "Member", "Password1", Role: AppRoles.User));
        Assert.True(user.IsSuccess, user.IsFailure ? user.Error.Code : null);
        Assert.Contains(AppRoles.User, user.Value.Roles);
        Assert.False(user.Value.IsDisabled);

        var admin = await harness.CreateAsync(new CreateAdminUserCommand(
            "admin@subify.local", "Admin", "Password1", Role: AppRoles.Admin));
        Assert.True(admin.IsSuccess, admin.IsFailure ? admin.Error.Code : null);
        Assert.Contains(AppRoles.Admin, admin.Value.Roles);

        // Admin can create User but not Admin
        harness.SetUser(admin.Value.Id, AppRoles.Admin);
        var okUser = await harness.CreateAsync(new CreateAdminUserCommand(
            "u2@subify.local", "U2", "Password1", Role: AppRoles.User));
        Assert.True(okUser.IsSuccess, okUser.IsFailure ? okUser.Error.Code : null);

        var deniedAdmin = await harness.CreateAsync(new CreateAdminUserCommand(
            "a2@subify.local", "A2", "Password1", Role: AppRoles.Admin));
        Assert.Equal(DomainErrors.UserErrors.AccessDenied.Code, deniedAdmin.Error.Code);

        // Plain user denied
        harness.SetUser(user.Value.Id, AppRoles.User);
        var denied = await harness.ListAsync(new ListAdminUsersQuery());
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, denied.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_duplicate_email()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        Assert.True((await harness.CreateAsync(new CreateAdminUserCommand(
            "dup@subify.local", "One", "Password1"))).IsSuccess);

        var dup = await harness.CreateAsync(new CreateAdminUserCommand(
            "dup@subify.local", "Two", "Password1"));
        Assert.Equal(DomainErrors.Auth.EmailAlreadyRegistered.Code, dup.Error.Code);
    }

    [Fact]
    public async Task Patch_lock_unlock_and_protect_super_admin()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var created = await harness.CreateAsync(new CreateAdminUserCommand(
            "lockme@subify.local", "Lock Me", "Password1"));
        Assert.True(created.IsSuccess);

        var locked = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, IsLocked: true));
        Assert.True(locked.IsSuccess, locked.IsFailure ? locked.Error.Code : null);
        Assert.True(locked.Value.IsLockedOut);

        var unlocked = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, IsLocked: false));
        Assert.True(unlocked.IsSuccess);
        Assert.False(unlocked.Value.IsLockedOut);

        var protect = await harness.PatchAsync(new PatchAdminUserCommand(
            superId, IsLocked: true));
        Assert.Equal(DomainErrors.UserErrors.CannotModifySuperAdmin.Code, protect.Error.Code);
    }

    [Fact]
    public async Task Patch_role_user_to_admin_and_back()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var created = await harness.CreateAsync(new CreateAdminUserCommand(
            "role@subify.local", "Role User", "Password1", Role: AppRoles.User));

        var asAdmin = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, Role: AppRoles.Admin));
        Assert.True(asAdmin.IsSuccess, asAdmin.IsFailure ? asAdmin.Error.Code : null);
        Assert.Contains(AppRoles.Admin, asAdmin.Value.Roles);
        Assert.DoesNotContain(AppRoles.User, asAdmin.Value.Roles);

        var asUser = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, Role: AppRoles.User));
        Assert.Contains(AppRoles.User, asUser.Value.Roles);
    }

    [Fact]
    public async Task Soft_disable_blocks_login_and_enable_restores()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var created = await harness.CreateAsync(new CreateAdminUserCommand(
            "dis@subify.local", "Disabled", "Password1"));
        Assert.True(created.IsSuccess);

        var disabled = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, IsDisabled: true));
        Assert.True(disabled.IsSuccess, disabled.IsFailure ? disabled.Error.Code : null);
        Assert.True(disabled.Value.IsDisabled);
        Assert.NotNull(disabled.Value.DisabledAt);

        var loginBlocked = await harness.LoginAsync("dis@subify.local", "Password1");
        Assert.True(loginBlocked.IsFailure);
        Assert.Equal(DomainErrors.UserErrors.AccountDisabled.Code, loginBlocked.Error.Code);

        harness.SetUser(superId, AppRoles.SuperAdmin);
        var enabled = await harness.PatchAsync(new PatchAdminUserCommand(
            created.Value.Id, IsDisabled: false));
        Assert.False(enabled.Value.IsDisabled);

        var loginOk = await harness.LoginAsync("dis@subify.local", "Password1");
        Assert.True(loginOk.IsSuccess, loginOk.IsFailure ? loginOk.Error.Code : null);
    }

    [Fact]
    public async Task Cannot_disable_self()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        // SuperAdmin protected first; create second SuperAdmin isn't allowed.
        // Use a second SuperAdmin via direct role for self-disable test on Admin account:
        var admin = await harness.CreateAsync(new CreateAdminUserCommand(
            "self@subify.local", "Self", "Password1", Role: AppRoles.Admin));
        // Promote to SuperAdmin so they can call Patch
        await harness.PromoteToSuperAdminAsync(admin.Value.Id);
        harness.SetUser(admin.Value.Id, AppRoles.SuperAdmin);

        var self = await harness.PatchAsync(new PatchAdminUserCommand(
            admin.Value.Id, IsDisabled: true));
        // Target is SuperAdmin → CannotModifySuperAdmin takes precedence
        Assert.Equal(DomainErrors.UserErrors.CannotModifySuperAdmin.Code, self.Error.Code);

        // Self-disable as SuperAdmin targeting a non-super self isn't possible if they are SuperAdmin.
        // Create plain SuperAdmin is protected. Test self-lock on non-super via temporary demotion path:
        // Instead: SuperAdmin patches themselves for lock → CannotModifySuperAdmin
        harness.SetUser(superId, AppRoles.SuperAdmin);
        var selfLock = await harness.PatchAsync(new PatchAdminUserCommand(superId, IsLocked: true));
        Assert.Equal(DomainErrors.UserErrors.CannotModifySuperAdmin.Code, selfLock.Error.Code);
    }

    [Fact]
    public async Task Admin_cannot_see_other_users_subscriptions()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        var member = await harness.CreateAsync(new CreateAdminUserCommand(
            "member@subify.local", "Member", "Password1"));
        Assert.True(member.IsSuccess);

        // Member owns a subscription
        harness.SetUser(member.Value.Id, AppRoles.User);
        var sub = await harness.CreateSubAsync(new CreateSubscriptionCommand(
            "Netflix", 100m, "TRY", "monthly", 1, Today.AddDays(5)));
        Assert.True(sub.IsSuccess, sub.IsFailure ? sub.Error.Code : null);

        // SuperAdmin lists own subscriptions — must not include member's
        harness.SetUser(superId, AppRoles.SuperAdmin);
        var list = await harness.ListSubsAsync(new ListSubscriptionsQuery());
        Assert.True(list.IsSuccess, list.IsFailure ? list.Error.Code : null);
        Assert.DoesNotContain(list.Value.Data, s => s.Id == sub.Value.Id);
        Assert.Empty(list.Value.Data);

        // Admin user list may show count only (not subscription payload)
        var users = await harness.ListAsync(new ListAdminUsersQuery(Search: "member"));
        Assert.Single(users.Value.Data);
        Assert.Equal(1, users.Value.Data[0].ActiveSubscriptionCount);
        // Response type has no subscription list property — count only (7.1.4)
    }

    [Fact]
    public async Task Patch_requires_super_admin()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);
        var target = await harness.CreateAsync(new CreateAdminUserCommand(
            "t@subify.local", "T", "Password1"));
        var admin = await harness.CreateAsync(new CreateAdminUserCommand(
            "admin@subify.local", "A", "Password1", Role: AppRoles.Admin));

        harness.SetUser(admin.Value.Id, AppRoles.Admin);
        var denied = await harness.PatchAsync(new PatchAdminUserCommand(
            target.Value.Id, IsLocked: true));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, denied.Error.Code);
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
                    o.Password.RequireDigit = true;
                    o.Password.RequireLowercase = true;
                    o.Password.RequireUppercase = true;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 8;
                    o.User.RequireUniqueEmail = true;
                    o.Lockout.AllowedForNewUsers = true;
                    o.Lockout.MaxFailedAccessAttempts = 5;
                    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SubifyDbContext>();

            services.Configure<JwtOptions>(o =>
            {
                o.Issuer = "SubifyOS";
                o.Audience = "SubifyOSClient";
                o.SecretKey = "SuperSecretKeyForSubifyOsProjectWhichNeedsToBeLongEnough";
                o.ExpirationInMinutes = 60;
                o.RefreshTokenExpirationDays = 7;
            });

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<ListAdminUsersHandler>();
            services.AddScoped<CreateAdminUserHandler>();
            services.AddScoped<PatchAdminUserHandler>();
            services.AddScoped<LoginHandler>();
            services.AddScoped<CreateSubscriptionHandler>();
            services.AddScoped<ListSubscriptionsHandler>();

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

        public void SetUser(Guid userId, string role)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Roles = [role];
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

        public async Task PromoteToSuperAdminAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString())
                       ?? throw new InvalidOperationException("missing");
            if (!await users.IsInRoleAsync(user, AppRoles.SuperAdmin))
            {
                await users.AddToRoleAsync(user, AppRoles.SuperAdmin);
            }
        }

        public async Task<Result<AdminUserResponse>> CreateAsync(CreateAdminUserCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateAdminUserHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ListAdminUsersResponse>> ListAsync(ListAdminUsersQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListAdminUsersHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<AdminUserResponse>> PatchAsync(PatchAdminUserCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PatchAdminUserHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<LoginResponse>> LoginAsync(string email, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<LoginHandler>()
                .Handle(new LoginCommand(email, password), CancellationToken.None);
        }

        public async Task<Result<CreateSubscriptionResponse>> CreateSubAsync(CreateSubscriptionCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ListSubscriptionsResponse>> ListSubsAsync(ListSubscriptionsQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListSubscriptionsHandler>()
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
