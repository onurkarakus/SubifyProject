using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Application.Features.Admin.Settings.TestSmtp;
using Subify.Application.Features.Auth.ForgotPassword;
using Subify.Application.Features.Auth.ResetPasswordWithToken;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Email;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 15 — forgot/reset + test-smtp + template service (no real SMTP).</summary>
public class EmailHandlerTests
{
    [Fact]
    public async Task Forgot_password_unknown_email_still_succeeds()
    {
        await using var harness = await Harness.CreateAsync();
        var result = await harness.ForgotAsync("ghost@subify.local");
        Assert.True(result.IsSuccess);
        Assert.Empty(harness.Sender.Sent);
    }

    [Fact]
    public async Task Forgot_password_sends_when_smtp_configured()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: true);
        await harness.SeedUserAsync("user@subify.local");

        var result = await harness.ForgotAsync("user@subify.local");
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Single(harness.Sender.Sent);
        Assert.False(string.IsNullOrWhiteSpace(harness.Sender.Sent[0].Subject));
        Assert.Contains("http", harness.Sender.Sent[0].HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reset_password_with_invalid_token_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SeedUserAsync("user@subify.local");

        var result = await harness.ResetAsync(
            "user@subify.local",
            "bad-token",
            "NewPassword1");

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Auth.InvalidResetCode.Code, result.Error.Code);
    }

    [Fact]
    public async Task Reset_password_with_valid_token_succeeds()
    {
        await using var harness = await Harness.CreateAsync();
        var userId = await harness.SeedUserAsync("user@subify.local", password: "OldPassword1");

        string token;
        using (var scope = harness.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            token = await users.GeneratePasswordResetTokenAsync(user);
        }

        var result = await harness.ResetAsync("user@subify.local", token, "NewPassword1");
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);

        using (var scope = harness.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("user@subify.local");
            Assert.NotNull(user);
            Assert.True(await users.CheckPasswordAsync(user, "NewPassword1"));
        }
    }

    [Fact]
    public async Task Test_smtp_requires_super_admin_and_config()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: false);

        var unauth = await harness.TestSmtpAsync();
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, unauth.Error.Code);

        var adminId = await harness.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        harness.SetUser(adminId, AppRoles.SuperAdmin, "super@subify.local");

        var noSmtp = await harness.TestSmtpAsync();
        Assert.Equal(DomainErrors.SystemSettingsErrors.SmtpNotConfigured.Code, noSmtp.Error.Code);

        await harness.EnableSmtpAsync();
        var ok = await harness.TestSmtpAsync("probe@subify.local");
        Assert.True(ok.IsSuccess, ok.IsFailure ? ok.Error.Code : null);
        Assert.Contains(harness.Sender.Sent, m => m.ToEmail == "probe@subify.local");
    }

    [Fact]
    public async Task Template_service_renders_catalog_fallback()
    {
        await using var harness = await Harness.CreateAsync();
        using var scope = harness.CreateScope();
        var templates = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();

        var rendered = await templates.RenderAsync(
            SystemEmailTemplates.Names.ResetPassword,
            "en",
            new Dictionary<string, string>
            {
                ["FullName"] = "Ada",
                ["ResetUrl"] = "https://x/reset"
            });

        Assert.True(rendered.IsSuccess);
        Assert.Contains("Ada", rendered.Value.HtmlBody);
        Assert.Contains("https://x/reset", rendered.Value.HtmlBody);
    }

    [Fact]
    public async Task Delivery_dedupes_second_send()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: true);
        using var scope = harness.CreateScope();
        var delivery = scope.ServiceProvider.GetRequiredService<IEmailDeliveryService>();

        var tokens = new Dictionary<string, string>
        {
            ["FullName"] = "A",
            ["ResetUrl"] = "http://x",
            ["SubscriptionName"] = "Netflix",
            ["Amount"] = "10",
            ["Currency"] = "TRY",
            ["RenewalDate"] = "2026-08-10",
            ["AppUrl"] = "http://localhost:3000",
            ["InviterName"] = "Admin",
            ["InstanceName"] = "Lab",
            ["InviteEmail"] = "a@b.com",
            ["InviteUrl"] = "http://i"
        };

        var key = "test-dedupe-1";
        var first = await delivery.SendTemplatedAsync(
            SystemEmailTemplates.Names.ResetPassword,
            "en",
            "a@b.com",
            tokens,
            dedupeKey: key);
        var second = await delivery.SendTemplatedAsync(
            SystemEmailTemplates.Names.ResetPassword,
            "en",
            "a@b.com",
            tokens,
            dedupeKey: key);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(harness.Sender.Sent);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        public RecordingEmailSender Sender { get; }

        private Harness(SqliteConnection connection, ServiceProvider provider, RecordingEmailSender sender)
        {
            _connection = connection;
            _provider = provider;
            Sender = sender;
        }

        public static async Task<Harness> CreateAsync(bool smtpConfigured = false)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var sender = new RecordingEmailSender { Configured = smtpConfigured };
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
                .AddEntityFrameworkStores<SubifyDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<AppOptions>(o =>
            {
                o.PublicWebBaseUrl = "http://localhost:3000";
                o.ResetPasswordPathTemplate = "/reset-password?email={email}&token={token}";
            });

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddSingleton<IEmailSender>(sender);
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ForgotPasswordHandler>();
            services.AddScoped<ResetPasswordWithTokenHandler>();
            services.AddScoped<TestSmtpHandler>();

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

                if (!await db.SystemSettings.AnyAsync())
                {
                    var s = SystemSettings.CreateDefault();
                    if (smtpConfigured)
                    {
                        s.UpdateSmtp(
                            smtpEnabled: true,
                            smtpHost: "localhost",
                            smtpPort: 1025,
                            smtpFromEmail: "noreply@subify.local");
                    }

                    db.SystemSettings.Add(s);
                    await db.SaveChangesAsync();
                }
            }

            return new Harness(connection, provider, sender);
        }

        public IServiceScope CreateScope() => _provider.CreateScope();

        public void SetUser(Guid userId, string role, string email)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.IsAuthenticated = true;
            fake.UserId = userId;
            fake.Email = email;
            fake.Roles = [role];
        }

        public async Task EnableSmtpAsync()
        {
            Sender.Configured = true;
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var s = await db.SystemSettings.SingleAsync();
            s.UpdateSmtp(
                smtpEnabled: true,
                smtpHost: "localhost",
                smtpPort: 1025,
                smtpFromEmail: "noreply@subify.local");
            await db.SaveChangesAsync();
        }

        public async Task<Guid> SeedUserAsync(
            string email,
            string role = AppRoles.User,
            string password = "Password1")
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            var created = await users.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join(",", created.Errors.Select(e => e.Code)));
            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result> ForgotAsync(string email)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ForgotPasswordHandler>()
                .Handle(new ForgotPasswordCommand(email), CancellationToken.None);
        }

        public async Task<Result> ResetAsync(string email, string token, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ResetPasswordWithTokenHandler>()
                .Handle(new ResetPasswordWithTokenCommand(email, token, password), CancellationToken.None);
        }

        public async Task<Result> TestSmtpAsync(string? to = null)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<TestSmtpHandler>()
                .Handle(new TestSmtpCommand(to), CancellationToken.None);
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

        public sealed class RecordingEmailSender : IEmailSender
        {
            public bool Configured { get; set; }
            public List<EmailMessage> Sent { get; } = [];

            public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(Configured);

            public Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                if (!Configured)
                {
                    return Task.FromResult(Result.Failure(DomainErrors.SystemSettingsErrors.SmtpNotConfigured));
                }

                Sent.Add(message);
                return Task.FromResult(Result.Success());
            }
        }
    }
}
