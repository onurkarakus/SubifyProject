using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Email;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>15.3.1 / 15.3.2 — renewal window selection + dedupe.</summary>
public class RenewalReminderServiceTests
{
    [Fact]
    public async Task Skips_when_smtp_not_configured()
    {
        await using var h = await Harness.CreateAsync(smtp: false);
        var userId = await h.SeedUserWithEmailPrefsAsync(days: 7, emailEnabled: true);
        await h.SeedSubAsync(userId, daysUntilRenewal: 3);

        var n = await h.ProcessAsync();
        Assert.Equal(0, n);
        Assert.Empty(h.Sender.Sent);
    }

    [Fact]
    public async Task Skips_when_email_notifications_disabled()
    {
        await using var h = await Harness.CreateAsync(smtp: true);
        var userId = await h.SeedUserWithEmailPrefsAsync(days: 7, emailEnabled: false);
        await h.SeedSubAsync(userId, daysUntilRenewal: 2);

        var n = await h.ProcessAsync();
        Assert.Equal(0, n);
        Assert.Empty(h.Sender.Sent);
    }

    [Fact]
    public async Task Sends_for_subscription_inside_window()
    {
        await using var h = await Harness.CreateAsync(smtp: true);
        var userId = await h.SeedUserWithEmailPrefsAsync(days: 5, emailEnabled: true);
        await h.SeedSubAsync(userId, daysUntilRenewal: 3, name: "Netflix");

        var n = await h.ProcessAsync();
        Assert.Equal(1, n);
        Assert.Single(h.Sender.Sent);
        Assert.Contains("Netflix", h.Sender.Sent[0].HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Skips_subscription_outside_window()
    {
        await using var h = await Harness.CreateAsync(smtp: true);
        var userId = await h.SeedUserWithEmailPrefsAsync(days: 3, emailEnabled: true);
        await h.SeedSubAsync(userId, daysUntilRenewal: 14);

        var n = await h.ProcessAsync();
        Assert.Equal(0, n);
        Assert.Empty(h.Sender.Sent);
    }

    [Fact]
    public async Task Dedupe_prevents_second_send_same_renewal_date()
    {
        await using var h = await Harness.CreateAsync(smtp: true);
        var userId = await h.SeedUserWithEmailPrefsAsync(days: 7, emailEnabled: true);
        await h.SeedSubAsync(userId, daysUntilRenewal: 1);

        Assert.Equal(1, await h.ProcessAsync());
        Assert.Equal(1, await h.ProcessAsync()); // success via dedupe
        Assert.Single(h.Sender.Sent);
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

        public static async Task<Harness> CreateAsync(bool smtp)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var sender = new RecordingEmailSender { Configured = smtp };

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

            services.Configure<AppOptions>(o => o.PublicWebBaseUrl = "http://localhost:3000");
            services.AddSingleton<IEmailSender>(sender);
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
            services.AddScoped<IRenewalReminderService, RenewalReminderService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());

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
                    if (smtp)
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

        public async Task<int> ProcessAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IRenewalReminderService>()
                .ProcessDueRemindersAsync();
        }

        public async Task<Guid> SeedUserWithEmailPrefsAsync(int days, bool emailEnabled)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();

            var email = $"u{Guid.NewGuid():N}@subify.local";
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile("User", email);
            user.EmailConfirmed = true;
            Assert.True((await users.CreateAsync(user, "Password1")).Succeeded);
            await users.AddToRoleAsync(user, AppRoles.User);

            var prefs = NotificationSetting.CreateDefaults(user.Id);
            prefs.UpdateSettings(emailEnabled, pushEnabled: false, daysBeforeRenewal: days);
            db.NotificationSettings.Add(prefs);
            await db.SaveChangesAsync();
            return user.Id;
        }

        public async Task SeedSubAsync(Guid userId, int daysUntilRenewal, string name = "Sub")
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var renewal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysUntilRenewal));
            var created = Subscription.Create(
                userId: userId,
                name: name,
                price: 99m,
                currency: "TRY",
                billingCycle: BillingCycle.Monthly,
                sharedWithCount: 1,
                nextRenewalDate: renewal);
            Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);
            db.Subscriptions.Add(created.Value);
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
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
