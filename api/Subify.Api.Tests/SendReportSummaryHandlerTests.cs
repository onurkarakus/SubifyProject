using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Application.Features.Reports.SendReportSummary;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Email;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>POST /api/reports/email-summary — SMTP period summary.</summary>
public class SendReportSummaryHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Requires_smtp()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: false);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);

        var result = await harness.SendAsync(new SendReportSummaryCommand(Months: 6, Lang: "en"));
        Assert.Equal(DomainErrors.SystemSettingsErrors.SmtpNotConfigured.Code, result.Error.Code);
        Assert.Empty(harness.Sender.Sent);
    }

    [Fact]
    public async Task Requires_subscription_data()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: true);
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.SendAsync(new SendReportSummaryCommand(Months: 6, Lang: "en"));
        Assert.Equal(DomainErrors.ReportErrors.InsufficientData.Code, result.Error.Code);
        Assert.Empty(harness.Sender.Sent);
    }

    [Fact]
    public async Task Success_sends_templated_mail_and_activity()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: true);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);
        await harness.CreateSubAsync(userId, "Spotify", 50m);

        var result = await harness.SendAsync(new SendReportSummaryCommand(Months: 6, Lang: "en"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("u@subify.local", result.Value.ToEmail);
        Assert.Equal(6, result.Value.Months);
        Assert.Single(harness.Sender.Sent);
        Assert.Equal("u@subify.local", harness.Sender.Sent[0].ToEmail);
        Assert.Contains("summary", harness.Sender.Sent[0].Subject, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            harness.Sender.Sent[0].HtmlBody.Contains("Average", StringComparison.OrdinalIgnoreCase)
            || harness.Sender.Sent[0].HtmlBody.Contains("monthly", StringComparison.OrdinalIgnoreCase));

        var activities = await harness.GetActivityAsync(userId);
        Assert.Contains(activities, a => a.Action == ActivityLogConstants.Actions.ReportEmailSummary);
    }

    [Fact]
    public async Task Dedupe_second_send_same_day_skips_smtp()
    {
        await using var harness = await Harness.CreateAsync(smtpConfigured: true);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);

        Assert.True((await harness.SendAsync(new SendReportSummaryCommand(Months: 6, Lang: "en"))).IsSuccess);
        Assert.True((await harness.SendAsync(new SendReportSummaryCommand(Months: 6, Lang: "en"))).IsSuccess);
        Assert.Single(harness.Sender.Sent);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        private Harness(SqliteConnection connection, ServiceProvider provider, RecordingEmailSender sender)
        {
            _connection = connection;
            _provider = provider;
            Sender = sender;
        }

        public RecordingEmailSender Sender { get; }

        public static async Task<Harness> CreateAsync(bool smtpConfigured)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var sender = new RecordingEmailSender { Configured = smtpConfigured };
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

            services.Configure<AppOptions>(o => o.PublicWebBaseUrl = "http://localhost:3000");
            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddSingleton<IEmailSender>(sender);
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<SendReportSummaryHandler>();
            services.AddScoped<CreateSubscriptionHandler>();

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
                if (smtpConfigured)
                {
                    settings.UpdateSmtp(
                        smtpEnabled: true,
                        smtpHost: "localhost",
                        smtpPort: 1025,
                        smtpFromEmail: "noreply@subify.local");
                }

                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();
            }

            return new Harness(connection, provider, sender);
        }

        public void SetUser(Guid userId)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.UserId = userId;
            fake.IsAuthenticated = true;
            fake.Locale = "en";
            fake.Roles = [AppRoles.User];
        }

        public async Task<Guid> SeedUserAsync(string email)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            user.MainCurrency = "TRY";
            user.Locale = "en";
            var created = await users.CreateAsync(user, "Password1");
            Assert.True(created.Succeeded, string.Join(",", created.Errors.Select(e => e.Code)));
            await users.AddToRoleAsync(user, AppRoles.User);
            return user.Id;
        }

        public async Task CreateSubAsync(Guid userId, string name, decimal price)
        {
            SetUser(userId);
            await using var scope = _provider.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<CreateSubscriptionHandler>()
                .Handle(new CreateSubscriptionCommand(
                    name, price, "TRY", "monthly", 1, Today.AddDays(5)), CancellationToken.None);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error.Code);
            }
        }

        public async Task<Result<SendReportSummaryResponse>> SendAsync(SendReportSummaryCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<SendReportSummaryHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<List<ActivityLog>> GetActivityAsync(Guid userId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.ActivityLogs.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
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
