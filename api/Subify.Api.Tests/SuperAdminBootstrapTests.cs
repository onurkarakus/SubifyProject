using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Setup.CompleteSetup;
using Subify.Application.Features.Setup.CreateSetupAdmin;
using Subify.Application.Features.Setup.GetSetupStatus;
using Subify.Application.Features.Setup.UpdateSetupAi;
using Subify.Application.Features.Setup.UpdateSetupInstance;
using Subify.Application.Features.Setup.UpdateSetupSmtp;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>3.3 + 3S setup wizard API core.</summary>
public class SuperAdminBootstrapTests
{
    [Fact]
    public async Task CreateSetupAdmin_returns_tokens_and_SuperAdmin_role()
    {
        await using var harness = await SetupHarness.CreateAsync();

        var result = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal(AppRoles.SuperAdmin, result.Value.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
    }

    [Fact]
    public async Task CreateSetupAdmin_second_fails()
    {
        await using var harness = await SetupHarness.CreateAsync();
        Assert.True((await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"))).IsSuccess);

        var second = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Other", "other@subify.local", "Password1"));
        Assert.Equal(DomainErrors.Auth.SuperAdminAlreadyExists.Code, second.Error.Code);
    }

    [Fact]
    public async Task Status_before_admin_suggests_admin_step()
    {
        await using var harness = await SetupHarness.CreateAsync();

        var status = await harness.StatusAsync();
        Assert.True(status.IsSuccess);
        Assert.False(status.Value.IsSetupComplete);
        Assert.True(status.Value.CanCreateAdmin);
        Assert.Equal("admin", status.Value.SuggestedNextStep);
        Assert.False(status.Value.HasSmtpConfigured);
        Assert.False(status.Value.HasAiConfigured);
    }

    [Fact]
    public async Task Complete_setup_requires_super_admin_then_locks()
    {
        await using var harness = await SetupHarness.CreateAsync();
        await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));

        harness.SetCurrentUserAsSuperAdmin("owner@subify.local");

        var complete = await harness.CompleteAsync();
        Assert.True(complete.IsSuccess, complete.IsFailure ? complete.Error.Code : null);
        Assert.True(complete.Value.IsSetupComplete);

        var again = await harness.CompleteAsync();
        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, again.Error.Code);

        var adminAgain = await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "X", "x@subify.local", "Password1"));
        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, adminAgain.Error.Code);
    }

    [Fact]
    public async Task Update_instance_while_setup_open()
    {
        await using var harness = await SetupHarness.CreateAsync();
        await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));
        harness.SetCurrentUserAsSuperAdmin("owner@subify.local");

        var updated = await harness.UpdateInstanceAsync(new UpdateSetupInstanceCommand(
            InstanceName: "Home Lab",
            DefaultLocale: "en",
            DefaultCurrency: "USD",
            TimeZoneId: "UTC",
            AllowPublicRegistration: true));

        Assert.True(updated.IsSuccess);

        var status = await harness.StatusAsync();
        Assert.Equal("Home Lab", status.Value.InstanceName);
        Assert.True(status.Value.AllowPublicRegistration);
        Assert.Equal("en", status.Value.DefaultLocale);
        Assert.Equal("USD", status.Value.DefaultCurrency);
        Assert.Equal("instance", status.Value.SuggestedNextStep);
    }

    [Fact]
    public async Task Update_smtp_and_ai_while_setup_open()
    {
        await using var harness = await SetupHarness.CreateAsync();
        await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));
        harness.SetCurrentUserAsSuperAdmin("owner@subify.local");

        var smtp = await harness.UpdateSmtpAsync(new UpdateSetupSmtpCommand(
            SmtpEnabled: true,
            SmtpHost: "smtp.example.com",
            SmtpPort: 587,
            SmtpUser: "mailer",
            SmtpPassword: "secret",
            SmtpFromName: "Subify",
            SmtpFromEmail: "noreply@example.com"));
        Assert.True(smtp.IsSuccess, smtp.IsFailure ? smtp.Error.Code : null);

        var ai = await harness.UpdateAiAsync(new UpdateSetupAiCommand(
            AiProvider: "openai",
            AiApiKey: "sk-test",
            AiModel: "gpt-4o-mini"));
        Assert.True(ai.IsSuccess, ai.IsFailure ? ai.Error.Code : null);

        var status = await harness.StatusAsync();
        Assert.True(status.Value.HasSmtpConfigured);
        Assert.True(status.Value.HasAiConfigured);
        // Public status must not leak secrets — response DTO has no password/key fields
        Assert.DoesNotContain("secret", status.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sk-test", status.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Setup_mutations_blocked_after_complete()
    {
        await using var harness = await SetupHarness.CreateAsync();
        await harness.CreateAdminAsync(new CreateSetupAdminCommand(
            "Owner", "owner@subify.local", "Password1"));
        harness.SetCurrentUserAsSuperAdmin("owner@subify.local");
        Assert.True((await harness.CompleteAsync()).IsSuccess);

        var instance = await harness.UpdateInstanceAsync(new UpdateSetupInstanceCommand(
            "X", "tr", "TRY", null, false));
        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, instance.Error.Code);

        var smtp = await harness.UpdateSmtpAsync(new UpdateSetupSmtpCommand(
            true, "h", 25, null, null, null, null));
        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, smtp.Error.Code);

        var ai = await harness.UpdateAiAsync(new UpdateSetupAiCommand("p", "k", "m"));
        Assert.Equal(DomainErrors.Setup.AlreadyComplete.Code, ai.Error.Code);
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

        public static async Task<SetupHarness> CreateAsync()
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
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<CreateSetupAdminHandler>();
            services.AddScoped<GetSetupStatusHandler>();
            services.AddScoped<CompleteSetupHandler>();
            services.AddScoped<UpdateSetupInstanceHandler>();
            services.AddScoped<UpdateSetupSmtpHandler>();
            services.AddScoped<UpdateSetupAiHandler>();

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

            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext();

            return new SetupHarness(connection, provider);
        }

        public void SetCurrentUserAsSuperAdmin(string email)
        {
            using var scope = _provider.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = users.FindByEmailAsync(email).GetAwaiter().GetResult()
                       ?? throw new InvalidOperationException("user missing");
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = user.Id;
            fake.IsAuthenticated = true;
            fake.Roles = [AppRoles.SuperAdmin];
        }

        public async Task<Result<CreateSetupAdminResponse>> CreateAdminAsync(CreateSetupAdminCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateSetupAdminHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<SetupStatusResponse>> StatusAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetSetupStatusHandler>()
                .Handle(new GetSetupStatusQuery(), CancellationToken.None);
        }

        public async Task<Result<CompleteSetupResponse>> CompleteAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CompleteSetupHandler>()
                .Handle(new CompleteSetupCommand(), CancellationToken.None);
        }

        public async Task<Result> UpdateInstanceAsync(UpdateSetupInstanceCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateSetupInstanceHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result> UpdateSmtpAsync(UpdateSetupSmtpCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateSetupSmtpHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result> UpdateAiAsync(UpdateSetupAiCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateSetupAiHandler>()
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
