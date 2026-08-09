using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories;
using Subify.Application.Features.Categories.CreateUserCategory;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.1.3 — create user category.</summary>
public class CreateUserCategoryHandlerTests
{
    [Fact]
    public async Task Create_persists_for_current_user()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);

        var result = await harness.HandleAsync(new CreateUserCategoryCommand(
            "VPN Servisleri",
            "shield",
            "#6C5CE7"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("VPN Servisleri", result.Value.Name);
        Assert.Equal("shield", result.Value.Icon);
        Assert.Equal("#6C5CE7", result.Value.Color);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var row = await db.UserCategories.SingleAsync(c => c.Id == result.Value.Id);
        Assert.Equal(userId, row.UserId);
    }

    [Fact]
    public async Task Create_rejects_duplicate_name_case_insensitive()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        Assert.True((await harness.HandleAsync(new CreateUserCategoryCommand("Gym"))).IsSuccess);

        var dup = await harness.HandleAsync(new CreateUserCategoryCommand("gym"));
        Assert.Equal(DomainErrors.UserCategoryErrors.DuplicateName.Code, dup.Error.Code);
    }

    [Fact]
    public async Task Create_allows_same_name_for_different_users()
    {
        await using var harness = await Harness.CreateAsync();
        var a = await harness.SeedUserAsync("a@subify.local");
        var b = await harness.SeedUserAsync("b@subify.local");

        harness.SetUser(a);
        Assert.True((await harness.HandleAsync(new CreateUserCategoryCommand("Shared Name"))).IsSuccess);

        harness.SetUser(b);
        var other = await harness.HandleAsync(new CreateUserCategoryCommand("Shared Name"));
        Assert.True(other.IsSuccess, other.IsFailure ? other.Error.Code : null);
    }

    [Fact]
    public async Task Create_unauthenticated_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.HandleAsync(new CreateUserCategoryCommand("X"));
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
            services.AddScoped<CreateUserCategoryHandler>();

            var provider = services.BuildServiceProvider();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();
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

        public async Task<Result<UserCategoryResponse>> HandleAsync(CreateUserCategoryCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateUserCategoryHandler>()
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
