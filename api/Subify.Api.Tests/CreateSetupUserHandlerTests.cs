using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Setup.CreateSetupUser;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>3S.4.1 — create users during incomplete setup.</summary>
public class CreateSetupUserHandlerTests
{
    [Fact]
    public async Task SuperAdmin_creates_user_while_setup_open()
    {
        await using var h = await Harness.CreateAsync(setupComplete: false);
        var superId = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(superId, AppRoles.SuperAdmin);

        var result = await h.CreateAsync(new CreateSetupUserCommand(
            Email: "member@subify.local",
            FullName: "Member",
            Password: "Password1",
            Role: "User"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("member@subify.local", result.Value.Email);
        Assert.Contains(AppRoles.User, result.Value.Roles);

        using var scope = h.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await users.FindByEmailAsync("member@subify.local");
        Assert.NotNull(created);
        Assert.True(created.EmailConfirmed);
    }

    [Fact]
    public async Task SuperAdmin_can_create_admin_role()
    {
        await using var h = await Harness.CreateAsync(setupComplete: false);
        var superId = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(superId, AppRoles.SuperAdmin);

        var result = await h.CreateAsync(new CreateSetupUserCommand(
            "admin2@subify.local",
            "Admin Two",
            "Password1",
            "Admin"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Contains(AppRoles.Admin, result.Value.Roles);
    }

    [Fact]
    public async Task Fails_when_setup_already_complete()
    {
        await using var h = await Harness.CreateAsync(setupComplete: true);
        var superId = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(superId, AppRoles.SuperAdmin);

        var result = await h.CreateAsync(new CreateSetupUserCommand(
            "x@subify.local", "X", "Password1"));

        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, result.Error.Code);
    }

    [Fact]
    public async Task Non_super_admin_denied()
    {
        await using var h = await Harness.CreateAsync(setupComplete: false);
        var adminId = await h.SeedUserAsync("admin@subify.local", AppRoles.Admin);
        h.SetUser(adminId, AppRoles.Admin);

        var result = await h.CreateAsync(new CreateSetupUserCommand(
            "x@subify.local", "X", "Password1"));

        Assert.Equal(DomainErrors.UserErrors.AccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Duplicate_email_conflict()
    {
        await using var h = await Harness.CreateAsync(setupComplete: false);
        var superId = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(superId, AppRoles.SuperAdmin);
        await h.SeedUserAsync("dup@subify.local", AppRoles.User);

        var result = await h.CreateAsync(new CreateSetupUserCommand(
            "dup@subify.local", "Dup", "Password1"));

        Assert.Equal(DomainErrors.Auth.EmailAlreadyRegistered.Code, result.Error.Code);
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

        public static async Task<Harness> CreateAsync(bool setupComplete)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<SubifyDbContext>(o => o.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>(o =>
                {
                    o.Password.RequireDigit = true;
                    o.Password.RequireLowercase = true;
                    o.Password.RequireUppercase = true;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 8;
                    o.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SubifyDbContext>();

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<CreateSetupUserHandler>();

            var provider = services.BuildServiceProvider();
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

                var settings = SystemSettings.CreateDefault();
                if (setupComplete)
                {
                    settings.MarkSetupComplete();
                }

                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();
            }

            return new Harness(connection, provider);
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public void SetUser(Guid userId, string role)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.IsAuthenticated = true;
            fake.UserId = userId;
            fake.Email = "x@subify.local";
            fake.Roles = [role];
        }

        public async Task<Guid> SeedUserAsync(string email, string role)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await users.FindByEmailAsync(email);
            if (existing is not null)
            {
                return existing.Id;
            }

            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            Assert.True((await users.CreateAsync(user, "Password1")).Succeeded);
            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result<SetupUserResponse>> CreateAsync(CreateSetupUserCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSetupUserHandler>()
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
