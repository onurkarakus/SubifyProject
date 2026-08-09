using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Application.Common.Security;
using Subify.Application.Features.Admin.Invites;
using Subify.Application.Features.Admin.Invites.CreateInvite;
using Subify.Application.Features.Admin.Invites.ListInvites;
using Subify.Application.Features.Auth.AcceptInvite;
using Subify.Application.Features.Auth.Login;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Authentication;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 7.2 — create/list invites, accept-invite, single-use + expiry.</summary>
public class InviteHandlerTests
{
    [Fact]
    public async Task Create_returns_plain_token_and_url_once()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var result = await harness.CreateInviteAsync(new CreateInviteCommand("new@subify.local", ExpiryDays: 3));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Equal("new@subify.local", result.Value.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.Contains(result.Value.Token, result.Value.InviteUrl);
        Assert.StartsWith("http://localhost:3000/accept-invite?token=", result.Value.InviteUrl);
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow.AddDays(2));

        // Hash only in DB
        var stored = await harness.GetInviteByIdAsync(result.Value.Id);
        Assert.NotNull(stored);
        Assert.Equal(InviteTokenHasher.Hash(result.Value.Token), stored!.TokenHash);
        Assert.NotEqual(result.Value.Token, stored.TokenHash);
    }

    [Fact]
    public async Task Create_rejects_existing_user_and_non_admin()
    {
        await using var harness = await Harness.CreateAsync();
        var adminId = await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin);
        await harness.SeedUserAsync("exists@subify.local", AppRoles.User);
        harness.SetUser(adminId, AppRoles.SuperAdmin);

        var dup = await harness.CreateInviteAsync(new CreateInviteCommand("exists@subify.local"));
        Assert.Equal(DomainErrors.Auth.EmailAlreadyRegistered.Code, dup.Error.Code);

        var userId = await harness.SeedUserAsync("plain@subify.local", AppRoles.User);
        harness.SetUser(userId, AppRoles.User);
        var denied = await harness.CreateInviteAsync(new CreateInviteCommand("x@subify.local"));
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, denied.Error.Code);
    }

    [Fact]
    public async Task Create_supersedes_prior_pending_for_same_email()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var first = await harness.CreateInviteAsync(new CreateInviteCommand("same@subify.local"));
        var second = await harness.CreateInviteAsync(new CreateInviteCommand("same@subify.local"));
        Assert.True(first.IsSuccess && second.IsSuccess);

        var list = await harness.ListAsync(new ListInvitesQuery());
        Assert.Single(list.Value.Data);
        Assert.Equal(second.Value.Id, list.Value.Data[0].Id);

        // Old token no longer valid
        var acceptOld = await harness.AcceptAsync(new AcceptInviteCommand(
            first.Value.Token, "Old", "Password1"));
        Assert.Equal(DomainErrors.Auth.InvalidInviteToken.Code, acceptOld.Error.Code);
    }

    [Fact]
    public async Task List_pending_only_excludes_used_and_expired()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var live = await harness.CreateInviteAsync(new CreateInviteCommand("live@subify.local"));
        var toAccept = await harness.CreateInviteAsync(new CreateInviteCommand("used@subify.local"));
        Assert.True((await harness.AcceptAsync(new AcceptInviteCommand(
            toAccept.Value.Token, "Used User", "Password1"))).IsSuccess);

        await harness.SeedExpiredInviteAsync("old@subify.local");

        var pending = await harness.ListAsync(new ListInvitesQuery());
        Assert.True(pending.IsSuccess);
        Assert.Single(pending.Value.Data);
        Assert.Equal(live.Value.Id, pending.Value.Data[0].Id);
        Assert.True(pending.Value.Data[0].IsPending);

        var withExpired = await harness.ListAsync(new ListInvitesQuery(IncludeExpired: true));
        Assert.Equal(2, withExpired.Value.Data.Count); // live + expired unused
        Assert.Contains(withExpired.Value.Data, i => i.Email == "old@subify.local" && !i.IsPending);
    }

    [Fact]
    public async Task Accept_creates_user_and_is_single_use()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var invite = await harness.CreateInviteAsync(new CreateInviteCommand("member@subify.local"));
        Assert.True(invite.IsSuccess);

        var accept = await harness.AcceptAsync(new AcceptInviteCommand(
            invite.Value.Token, "Family Member", "Password1"));
        Assert.True(accept.IsSuccess, accept.IsFailure ? accept.Error.Code : null);
        Assert.Equal("member@subify.local", accept.Value.Email);
        Assert.Equal("Family Member", accept.Value.FullName);

        // Login works
        var login = await harness.LoginAsync("member@subify.local", "Password1");
        Assert.True(login.IsSuccess, login.IsFailure ? login.Error.Code : null);
        Assert.Contains(AppRoles.User, login.Value.User.Roles);

        // Second accept fails (7.2.5)
        var again = await harness.AcceptAsync(new AcceptInviteCommand(
            invite.Value.Token, "Again", "Password1"));
        Assert.Equal(DomainErrors.Auth.InvalidInviteToken.Code, again.Error.Code);

        // Pending list empty for that invite
        harness.SetUser(await harness.GetUserIdByEmailAsync("admin@subify.local"), AppRoles.SuperAdmin);
        var list = await harness.ListAsync(new ListInvitesQuery());
        Assert.DoesNotContain(list.Value.Data, i => i.Email == "member@subify.local");
    }

    [Fact]
    public async Task Accept_works_when_public_registration_disabled()
    {
        await using var harness = await Harness.CreateAsync(allowPublicRegistration: false);
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var invite = await harness.CreateInviteAsync(new CreateInviteCommand("invitee@subify.local"));
        var accept = await harness.AcceptAsync(new AcceptInviteCommand(
            invite.Value.Token, "Invitee", "Password1"));
        Assert.True(accept.IsSuccess, accept.IsFailure ? accept.Error.Code : null);
    }

    [Fact]
    public async Task Accept_expired_or_bad_token_fails()
    {
        await using var harness = await Harness.CreateAsync();
        harness.SetUser(await harness.SeedUserAsync("admin@subify.local", AppRoles.SuperAdmin), AppRoles.SuperAdmin);

        var bad = await harness.AcceptAsync(new AcceptInviteCommand(
            "not-a-real-token", "X", "Password1"));
        Assert.Equal(DomainErrors.Auth.InvalidInviteToken.Code, bad.Error.Code);

        var (expiredId, plain) = await harness.SeedExpiredInviteAsync("exp@subify.local");
        Assert.NotEqual(Guid.Empty, expiredId);

        var acceptExp = await harness.AcceptAsync(new AcceptInviteCommand(plain, "Exp", "Password1"));
        Assert.Equal(DomainErrors.Auth.InvalidInviteToken.Code, acceptExp.Error.Code);
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

        public static async Task<Harness> CreateAsync(bool allowPublicRegistration = true)
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
            services.Configure<AppOptions>(o =>
            {
                o.PublicWebBaseUrl = "http://localhost:3000";
                o.InvitePathTemplate = "/accept-invite?token={token}";
            });

            services.AddSingleton<ICurrentUserService, FakeCurrentUser>();
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IActivityLogger, Subify.Application.Common.Activity.ActivityLogger>();
            services.AddSingleton<IEmailSender, NoopEmailSenderForTests>();
            services.AddSingleton<IEmailDeliveryService, NoopDeliveryForTests>();
            services.AddScoped<CreateInviteHandler>();
            services.AddScoped<ListInvitesHandler>();
            services.AddScoped<AcceptInviteHandler>();
            services.AddScoped<LoginHandler>();

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
                settings.UpdateInstance(allowPublicRegistration: allowPublicRegistration);
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

        public async Task<Guid> GetUserIdByEmailAsync(string email)
        {
            await using var scope = _provider.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email)
                       ?? throw new InvalidOperationException("missing user");
            return user.Id;
        }

        public async Task<Result<CreateInviteResponse>> CreateInviteAsync(CreateInviteCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CreateInviteHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ListInvitesResponse>> ListAsync(ListInvitesQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ListInvitesHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<AcceptInviteResponse>> AcceptAsync(AcceptInviteCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<AcceptInviteHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<LoginResponse>> LoginAsync(string email, string password)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<LoginHandler>()
                .Handle(new LoginCommand(email, password), CancellationToken.None);
        }

        public async Task<UserInvite?> GetInviteByIdAsync(Guid id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            return await db.UserInvites.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<(Guid Id, string PlainToken)> SeedExpiredInviteAsync(string email)
        {
            var plainToken = InviteTokenHasher.GeneratePlainText();
            var hash = InviteTokenHasher.Hash(plainToken);
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SubifyDbContext>();
            var admin = await db.Users.FirstAsync();
            var invite = UserInvite.Create(
                email,
                hash,
                admin.Id,
                expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
            db.UserInvites.Add(invite);
            await db.SaveChangesAsync();
            return (invite.Id, plainToken);
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

    private sealed class NoopEmailSenderForTests : IEmailSender
    {
        public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure(DomainErrors.SystemSettingsErrors.SmtpNotConfigured));
    }

    private sealed class NoopDeliveryForTests : IEmailDeliveryService
    {
        public Task<Result> SendTemplatedAsync(
            string templateName,
            string? locale,
            string toEmail,
            IReadOnlyDictionary<string, string> tokens,
            Guid? userId = null,
            Guid? relatedEntityId = null,
            string? dedupeKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }
}
