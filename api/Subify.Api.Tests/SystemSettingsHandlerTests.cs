using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Admin.Settings;
using Subify.Application.Features.Admin.Settings.GetSystemSettings;
using Subify.Application.Features.Admin.Settings.UpdateSystemSettings;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 7.3 — GET/PUT system settings, secret masking, audit without secrets.</summary>
public class SystemSettingsHandlerTests
{
    [Fact]
    public async Task Get_masks_secrets_and_exposes_flags()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        await harness.SeedSecretsAsync(aiKey: "sk-secret-key", smtpPassword: "smtp-pass");

        var result = await harness.GetAsync();
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.True(result.Value.Ai.HasApiKey);
        Assert.Equal(SystemSettingsMapper.SecretMask, result.Value.Ai.ApiKeyMasked);
        Assert.DoesNotContain("sk-secret", result.Value.Ai.ApiKeyMasked ?? "");
        Assert.True(result.Value.Smtp.HasPassword);
        Assert.Equal(SystemSettingsMapper.SecretMask, result.Value.Smtp.PasswordMasked);
        Assert.Equal("Subify", result.Value.Instance.InstanceName);
    }

    [Fact]
    public async Task Get_without_secrets_has_null_masks()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var result = await harness.GetAsync();
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Ai.HasApiKey);
        Assert.Null(result.Value.Ai.ApiKeyMasked);
        Assert.False(result.Value.Smtp.HasPassword);
        Assert.Null(result.Value.Smtp.PasswordMasked);
    }

    [Fact]
    public async Task Get_requires_super_admin()
    {
        await using var harness = await Harness.CreateAsync();

        var unauth = await harness.GetAsync();
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, unauth.Error.Code);

        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.Admin), AppRoles.Admin);
        var denied = await harness.GetAsync();
        Assert.Equal(DomainErrors.SystemSettingsErrors.AccessDenied.Code, denied.Error.Code);
    }

    [Fact]
    public async Task Put_partial_update_preserves_secrets_when_omitted()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        await harness.SeedSecretsAsync(aiKey: "keep-me", smtpPassword: "keep-smtp");

        var updated = await harness.UpdateAsync(new UpdateSystemSettingsCommand(
            InstanceName: "Home Lab",
            DefaultLocale: "en",
            DefaultCurrency: "USD",
            AllowPublicRegistration: true,
            AiProvider: "openai",
            AiModel: "gpt-4o-mini",
            SmtpEnabled: true,
            SmtpHost: "smtp.example.com",
            SmtpPort: 587,
            SmtpFromEmail: "noreply@example.com"));

        Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Code : null);
        Assert.Equal("Home Lab", updated.Value.Instance.InstanceName);
        Assert.Equal("en", updated.Value.Instance.DefaultLocale);
        Assert.Equal("USD", updated.Value.Instance.DefaultCurrency);
        Assert.True(updated.Value.Instance.AllowPublicRegistration);
        Assert.Equal("openai", updated.Value.Ai.Provider);
        Assert.True(updated.Value.Ai.HasApiKey); // preserved
        Assert.True(updated.Value.Smtp.HasPassword); // preserved
        Assert.Equal("smtp.example.com", updated.Value.Smtp.Host);

        // Raw secrets still in DB
        var raw = await harness.LoadSettingsAsync();
        Assert.Equal("keep-me", raw.AiApiKey);
        Assert.Equal("keep-smtp", raw.SmtpPassword);
    }

    [Fact]
    public async Task Put_empty_secret_clears_and_non_empty_sets()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);
        await harness.SeedSecretsAsync(aiKey: "old-key", smtpPassword: "old-pass");

        var set = await harness.UpdateAsync(new UpdateSystemSettingsCommand(
            AiApiKey: "new-key",
            SmtpPassword: "new-pass"));
        Assert.True(set.IsSuccess);
        Assert.Equal("new-key", (await harness.LoadSettingsAsync()).AiApiKey);
        Assert.Equal("new-pass", (await harness.LoadSettingsAsync()).SmtpPassword);

        var clear = await harness.UpdateAsync(new UpdateSystemSettingsCommand(
            AiApiKey: "",
            SmtpPassword: ""));
        Assert.True(clear.IsSuccess);
        Assert.False(clear.Value.Ai.HasApiKey);
        Assert.False(clear.Value.Smtp.HasPassword);
        Assert.Null((await harness.LoadSettingsAsync()).AiApiKey);
        Assert.Null((await harness.LoadSettingsAsync()).SmtpPassword);
    }

    [Fact]
    public async Task Put_writes_audit_without_secret_values()
    {
        await using var harness = await Harness.CreateAsync();
        var superId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(superId, AppRoles.SuperAdmin);

        await harness.UpdateAsync(new UpdateSystemSettingsCommand(
            InstanceName: "Audited",
            AiApiKey: "super-secret-key-value",
            SmtpPassword: "super-secret-smtp"));

        var logs = await harness.GetActivityAsync(superId);
        Assert.NotEmpty(logs);
        var log = logs.Single(a => a.Action == ActivityLogConstants.Actions.SettingsUpdated);
        Assert.Equal(ActivityLogConstants.EntityTypes.SystemSettings, log.EntityType);
        Assert.Contains("Audited", log.NewValues ?? "");
        Assert.Contains("hasAiApiKey", log.NewValues ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret-key-value", log.NewValues ?? "");
        Assert.DoesNotContain("super-secret-smtp", log.NewValues ?? "");
        Assert.DoesNotContain("super-secret-key-value", log.OldValues ?? "");
    }

    [Fact]
    public async Task Put_requires_super_admin()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.Admin), AppRoles.Admin);

        var denied = await harness.UpdateAsync(new UpdateSystemSettingsCommand(InstanceName: "Nope"));
        Assert.Equal(DomainErrors.SystemSettingsErrors.AccessDenied.Code, denied.Error.Code);
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
            services.AddScoped<GetSystemSettingsHandler>();
            services.AddScoped<UpdateSystemSettingsHandler>();

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

                var settings = SystemSettings.CreateDefault();
                settings.MarkSetupComplete();
                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();
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

        public async Task SeedSecretsAsync(string aiKey, string smtpPassword)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var settings = await db.SystemSettings.SingleAsync();
            settings.UpdateAi(aiApiKey: aiKey);
            settings.UpdateSmtp(smtpPassword: smtpPassword);
            await db.SaveChangesAsync();
        }

        public async Task<Result<SystemSettingsResponse>> GetAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetSystemSettingsHandler>()
                .Handle(new GetSystemSettingsQuery(), CancellationToken.None);
        }

        public async Task<Result<SystemSettingsResponse>> UpdateAsync(UpdateSystemSettingsCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateSystemSettingsHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<SystemSettings> LoadSettingsAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.SystemSettings.AsNoTracking().SingleAsync();
        }

        public async Task<List<ActivityLog>> GetActivityAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.ActivityLogs.AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync();
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
