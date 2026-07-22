using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Features.Auth.Register;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Task 3.2.1 — register validation outcomes against Identity + SQLite.</summary>
public class RegisterHandlerTests
{
    [Fact]
    public async Task Register_creates_user_with_confirmed_email_and_User_role()
    {
        await using var harness = await RegisterHarness.CreateAsync();

        var result = await harness.HandleAsync(new RegisterCommand(
            "Ada Lovelace",
            "ada@subify.local",
            "Password1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("ada@subify.local", result.Value.Email);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.UserId));

        var user = await harness.Users.FindByEmailAsync("ada@subify.local");
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
        Assert.True(await harness.Users.IsInRoleAsync(user, AppRoles.User));
        Assert.False(await harness.Users.IsInRoleAsync(user, AppRoles.SuperAdmin));

        using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
        var notify = await db.NotificationSettings.SingleAsync(n => n.UserId == user!.Id);
        Assert.False(notify.EmailEnabled);
        Assert.Equal(3, notify.DaysBeforeRenewal);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_conflict()
    {
        await using var harness = await RegisterHarness.CreateAsync();

        var first = await harness.HandleAsync(new RegisterCommand(
            "First",
            "dup@subify.local",
            "Password1"));
        Assert.True(first.IsSuccess);

        var second = await harness.HandleAsync(new RegisterCommand(
            "Second",
            "dup@subify.local",
            "Password1"));

        Assert.True(second.IsFailure);
        Assert.Equal(DomainErrors.Auth.EmailAlreadyRegistered.Code, second.Error.Code);
        Assert.Equal(Domain.Shared.ErrorType.Conflict, second.Error.Type);
    }

    private sealed class RegisterHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private RegisterHarness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public UserManager<ApplicationUser> Users =>
            _provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        public IServiceScope CreateScope() => _provider.CreateScope();

        public static async Task<RegisterHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
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

            services.AddScoped<Application.Common.Interfaces.ISubifyDbContext>(sp =>
                sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<RegisterHandler>();

            var provider = services.BuildServiceProvider();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();

                // Allow public registration for tests (3.2.13 gate)
                var settings = SystemSettings.CreateDefault();
                settings.MarkSetupComplete();
                settings.UpdateInstance(allowPublicRegistration: true);
                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();

                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                foreach (var name in AppRoles.All)
                {
                    if (!await roles.RoleExistsAsync(name))
                    {
                        await roles.CreateAsync(new IdentityRole<Guid>(name) { Id = Guid.CreateVersion7() });
                    }
                }
            }

            return new RegisterHarness(connection, provider);
        }

        public async Task<Domain.Shared.Result<RegisterResponse>> HandleAsync(RegisterCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<RegisterHandler>();
            return await handler.Handle(command, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
