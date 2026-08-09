using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Admin.Jobs.RunRenewalReminders;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>8.1.3 — SuperAdmin manual renewal reminder run.</summary>
public class RunRenewalRemindersHandlerTests
{
    [Fact]
    public async Task SuperAdmin_runs_scan()
    {
        await using var h = await Harness.CreateAsync();
        var id = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(id, AppRoles.SuperAdmin);
        h.Reminders.NextCount = 2;

        var result = await h.RunAsync();
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(2, result.Value.ProcessedCount);
        Assert.Equal(1, h.Reminders.CallCount);
    }

    [Fact]
    public async Task Non_super_admin_denied()
    {
        await using var h = await Harness.CreateAsync();
        var id = await h.SeedUserAsync("admin@subify.local", AppRoles.Admin);
        h.SetUser(id, AppRoles.Admin);

        var result = await h.RunAsync();
        Assert.Equal(DomainErrors.SystemSettingsErrors.AccessDenied.Code, result.Error.Code);
        Assert.Equal(0, h.Reminders.CallCount);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        public FakeReminders Reminders { get; }

        private Harness(SqliteConnection connection, ServiceProvider provider, FakeReminders reminders)
        {
            _connection = connection;
            _provider = provider;
            Reminders = reminders;
        }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var reminders = new FakeReminders();

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
            services.AddSingleton<IRenewalReminderService>(reminders);
            services.AddScoped<RunRenewalRemindersHandler>();

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
            }

            return new Harness(connection, provider, reminders);
        }

        public void SetUser(Guid userId, string role)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.IsAuthenticated = true;
            fake.UserId = userId;
            fake.Roles = [role];
        }

        public async Task<Guid> SeedUserAsync(string email, string role)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            Assert.True((await users.CreateAsync(user, "Password1")).Succeeded);
            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result<RunRenewalRemindersResponse>> RunAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<RunRenewalRemindersHandler>()
                .Handle(new RunRenewalRemindersCommand(), CancellationToken.None);
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

        public sealed class FakeReminders : IRenewalReminderService
        {
            public int CallCount { get; private set; }
            public int NextCount { get; set; }

            public Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(NextCount);
            }
        }
    }
}
