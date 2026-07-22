using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Features.Setup.CreateSetupAdmin;
using Subify.Application.Features.Setup.GetSetupStatus;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Tasks 3.3.1 / 3.3.6 — setup SuperAdmin bootstrap.</summary>
public class SuperAdminBootstrapTests
{
    [Fact]
    public async Task CreateSetupAdmin_first_user_is_SuperAdmin()
    {
        await using var harness = await SetupHarness.CreateAsync();

        var result = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner",
            "owner@subify.local",
            "Password1"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(AppRoles.SuperAdmin, result.Value.Role);

        var user = await harness.Users.FindByEmailAsync("owner@subify.local");
        Assert.NotNull(user);
        Assert.True(await harness.Users.IsInRoleAsync(user!, AppRoles.SuperAdmin));
    }

    [Fact]
    public async Task CreateSetupAdmin_second_call_fails_when_SuperAdmin_exists()
    {
        await using var harness = await SetupHarness.CreateAsync();

        var first = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));
        Assert.True(first.IsSuccess);

        var second = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Other", "other@subify.local", "Password1"));

        Assert.True(second.IsFailure);
        Assert.Equal(DomainErrors.Auth.SuperAdminAlreadyExists.Code, second.Error.Code);
    }

    [Fact]
    public async Task GetSetupStatus_reflects_super_admin_and_flags()
    {
        await using var harness = await SetupHarness.CreateAsync();

        var before = await harness.StatusAsync();
        Assert.True(before.IsSuccess);
        Assert.False(before.Value.HasSuperAdmin);
        Assert.False(before.Value.IsSetupComplete);

        await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));

        var after = await harness.StatusAsync();
        Assert.True(after.Value.HasSuperAdmin);
    }

    private sealed class SetupHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private SetupHarness(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public UserManager<ApplicationUser> Users =>
            _provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        public static async Task<SetupHarness> CreateAsync()
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
            services.AddScoped<CreateSetupAdminHandler>();
            services.AddScoped<GetSetupStatusHandler>();

            var provider = services.BuildServiceProvider();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
                await db.Database.EnsureCreatedAsync();
                db.SystemSettings.Add(SystemSettings.CreateDefault());
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

            return new SetupHarness(connection, provider);
        }

        public async Task<Domain.Shared.Result<CreateSetupAdminResponse>> CreateAdminAsync(
            CreateSetupAdminCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<CreateSetupAdminHandler>();
            return await handler.Handle(command, CancellationToken.None);
        }

        public async Task<Domain.Shared.Result<SetupStatusResponse>> StatusAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<GetSetupStatusHandler>();
            return await handler.Handle(new GetSetupStatusQuery(), CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
