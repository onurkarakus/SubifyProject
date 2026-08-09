using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Application.Features.Admin.EmailTemplates;
using Subify.Application.Features.Admin.EmailTemplates.GetEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.ListEmailTemplates;
using Subify.Application.Features.Admin.EmailTemplates.PreviewEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.TestSendEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.UpdateEmailTemplate;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>7.4.1 / 7.4.2 — SuperAdmin email template list/update/preview/test-send.</summary>
public class EmailTemplateAdminHandlerTests
{
    [Fact]
    public async Task List_returns_seeded_templates_for_super_admin()
    {
        await using var h = await Harness.CreateAsync();
        h.SetSuperAdmin();

        var list = await h.ListAsync();
        Assert.True(list.IsSuccess);
        Assert.True(list.Value.Data.Count >= 6); // 3 names × 2 locales
        Assert.Contains(list.Value.Data, t => t.Name == SystemEmailTemplates.Names.ResetPassword);
    }

    [Fact]
    public async Task Update_changes_subject_and_body()
    {
        await using var h = await Harness.CreateAsync();
        h.SetSuperAdmin();
        var id = (await h.ListAsync()).Value.Data.First().Id;

        var updated = await h.UpdateAsync(id, "New Subject {{FullName}}", "<p>Hello {{FullName}}</p>");
        Assert.True(updated.IsSuccess, updated.IsFailure ? updated.Error.Code : null);
        Assert.Equal("New Subject {{FullName}}", updated.Value.Subject);

        var got = await h.GetAsync(id);
        Assert.Equal("<p>Hello {{FullName}}</p>", got.Value.Body);
    }

    [Fact]
    public async Task Preview_renders_sample_tokens()
    {
        await using var h = await Harness.CreateAsync();
        h.SetSuperAdmin();
        var reset = (await h.ListAsync()).Value.Data
            .First(t => t.Name == SystemEmailTemplates.Names.ResetPassword && t.LanguageCode == "en");

        var preview = await h.PreviewAsync(reset.Id);
        Assert.True(preview.IsSuccess);
        Assert.DoesNotContain("{{FullName}}", preview.Value.HtmlBody);
        Assert.Contains("Ada", preview.Value.HtmlBody);
        Assert.Contains("Reset", preview.Value.Subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test_send_requires_smtp()
    {
        await using var h = await Harness.CreateAsync(smtp: false);
        h.SetSuperAdmin();
        var id = (await h.ListAsync()).Value.Data.First().Id;

        var result = await h.TestSendAsync(id, "probe@subify.local");
        Assert.Equal(DomainErrors.SystemSettingsErrors.SmtpNotConfigured.Code, result.Error.Code);
    }

    [Fact]
    public async Task Test_send_sends_when_smtp_configured()
    {
        await using var h = await Harness.CreateAsync(smtp: true);
        h.SetSuperAdmin();
        var id = (await h.ListAsync()).Value.Data
            .First(t => t.Name == SystemEmailTemplates.Names.Invite && t.LanguageCode == "en").Id;

        var result = await h.TestSendAsync(id, "probe@subify.local");
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Single(h.Sender.Sent);
        Assert.StartsWith("[TEST]", h.Sender.Sent[0].Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("probe@subify.local", h.Sender.Sent[0].ToEmail);
    }

    [Fact]
    public async Task Non_super_admin_denied()
    {
        await using var h = await Harness.CreateAsync();
        var adminId = await h.SeedUserAsync("admin@subify.local", AppRoles.Admin);
        h.SetUser(adminId, AppRoles.Admin, "admin@subify.local");

        var list = await h.ListAsync();
        Assert.Equal(DomainErrors.SystemSettingsErrors.AccessDenied.Code, list.Error.Code);
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

        public static async Task<Harness> CreateAsync(bool smtp = false)
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
            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddSingleton<IEmailSender>(sender);
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ListEmailTemplatesHandler>();
            services.AddScoped<GetEmailTemplateHandler>();
            services.AddScoped<UpdateEmailTemplateHandler>();
            services.AddScoped<PreviewEmailTemplateHandler>();
            services.AddScoped<TestSendEmailTemplateHandler>();

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
                }

                foreach (var def in SystemEmailTemplates.All)
                {
                    db.EmailTemplates.Add(EmailTemplates.Create(
                        def.Name, def.LanguageCode, def.Subject, def.Body));
                }

                await db.SaveChangesAsync();
            }

            return new Harness(connection, provider, sender);
        }

        public void SetSuperAdmin()
        {
            // seed super on first call
            SetUserAsync(AppRoles.SuperAdmin).GetAwaiter().GetResult();
        }

        private async Task SetUserAsync(string role)
        {
            var id = await SeedUserAsync($"super-{role}@subify.local", role);
            SetUser(id, role, $"super-{role}@subify.local");
        }

        public void SetUser(Guid userId, string role, string email)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.IsAuthenticated = true;
            fake.UserId = userId;
            fake.Email = email;
            fake.Roles = [role];
        }

        public async Task<Guid> SeedUserAsync(string email, string role)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await users.FindByEmailAsync(email);
            if (existing is not null)
            {
                return existing.Id;
            }

            var user = new ApplicationUser { Id = Guid.CreateVersion7() };
            user.ApplyRegistrationProfile(email.Split('@')[0], email);
            user.EmailConfirmed = true;
            Assert.True((await users.CreateAsync(user, "Password1")).Succeeded);
            await users.AddToRoleAsync(user, role);
            return user.Id;
        }

        public async Task<Result<ListEmailTemplatesResponse>> ListAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListEmailTemplatesHandler>()
                .Handle(new ListEmailTemplatesQuery(), CancellationToken.None);
        }

        public async Task<Result<EmailTemplateResponse>> GetAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetEmailTemplateHandler>()
                .Handle(new GetEmailTemplateQuery(id), CancellationToken.None);
        }

        public async Task<Result<EmailTemplateResponse>> UpdateAsync(Guid id, string subject, string body)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<UpdateEmailTemplateHandler>()
                .Handle(new UpdateEmailTemplateCommand(id, subject, body), CancellationToken.None);
        }

        public async Task<Result<PreviewEmailTemplateResponse>> PreviewAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PreviewEmailTemplateHandler>()
                .Handle(new PreviewEmailTemplateCommand(id), CancellationToken.None);
        }

        public async Task<Result> TestSendAsync(Guid id, string? to)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<TestSendEmailTemplateHandler>()
                .Handle(new TestSendEmailTemplateCommand(id, to), CancellationToken.None);
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
