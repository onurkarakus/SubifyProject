using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories;
using Subify.Application.Features.Categories.UpdateUserCategory;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 5.1.4 — update user category with ownership.</summary>
public class UpdateUserCategoryHandlerTests
{
    [Fact]
    public async Task Update_own_category_persists()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var id = await harness.SeedCategoryAsync(userId, "Old", "a", "#111");

        var result = await harness.HandleAsync(new UpdateUserCategoryCommand(
            id, "New Name", "b", "#222"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal("b", result.Value.Icon);
        Assert.Equal("#222", result.Value.Color);

        using var scope = harness.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<SubifyDbContext>()
            .UserCategories.SingleAsync(c => c.Id == id);
        Assert.Equal("New Name", row.Name);
    }

    [Fact]
    public async Task Update_missing_returns_not_found()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.HandleAsync(new UpdateUserCategoryCommand(
            Guid.CreateVersion7(), "X"));
        Assert.Equal(DomainErrors.UserCategoryErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_foreign_returns_access_denied()
    {
        await using var harness = await Harness.CreateAsync();
        var owner = await harness.SeedUserAsync("owner@subify.local");
        var other = await harness.SeedUserAsync("other@subify.local");
        var id = await harness.SeedCategoryAsync(owner, "Private");

        harness.SetUser(other);
        var result = await harness.HandleAsync(new UpdateUserCategoryCommand(id, "Hacked"));
        Assert.Equal(DomainErrors.UserCategoryErrors.AccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_rejects_duplicate_name_of_sibling()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.SeedCategoryAsync(userId, "A");
        var b = await harness.SeedCategoryAsync(userId, "B");

        var result = await harness.HandleAsync(new UpdateUserCategoryCommand(b, "a"));
        Assert.Equal(DomainErrors.UserCategoryErrors.DuplicateName.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_same_name_on_self_allowed()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        var id = await harness.SeedCategoryAsync(userId, "Keep");

        var result = await harness.HandleAsync(new UpdateUserCategoryCommand(id, "Keep", "new-icon"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("new-icon", result.Value.Icon);
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
            services.AddScoped<UpdateUserCategoryHandler>();

            var provider = services.BuildServiceProvider();
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

        public async Task<Guid> SeedCategoryAsync(
            Guid userId,
            string name,
            string? icon = null,
            string? color = null)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var c = UserCategory.CreateForUser(userId, name, icon, color);
            db.UserCategories.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<Result<UserCategoryResponse>> HandleAsync(UpdateUserCategoryCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateUserCategoryHandler>()
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
