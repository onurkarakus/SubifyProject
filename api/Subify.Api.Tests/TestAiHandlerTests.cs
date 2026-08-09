using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Admin.Settings.TestAi;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>7.3.4 — SuperAdmin AI connectivity probe.</summary>
public class TestAiHandlerTests
{
    [Fact]
    public async Task Unauthenticated_fails()
    {
        await using var h = await Harness.CreateAsync();
        var result = await h.TestAsync();
        Assert.Equal(DomainErrors.UserErrors.UnAuthorized.Code, result.Error.Code);
    }

    [Fact]
    public async Task Non_super_admin_denied()
    {
        await using var h = await Harness.CreateAsync();
        var id = await h.SeedUserAsync("admin@subify.local", AppRoles.Admin);
        h.SetUser(id, AppRoles.Admin);

        var result = await h.TestAsync();
        Assert.Equal(DomainErrors.SystemSettingsErrors.AccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Missing_api_key_returns_AI_KEY_MISSING()
    {
        await using var h = await Harness.CreateAsync(hasKey: false);
        var id = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(id, AppRoles.SuperAdmin);

        var result = await h.TestAsync();
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AiErrors.ApiKeyMissing.Code, result.Error.Code);
    }

    [Fact]
    public async Task SuperAdmin_with_key_returns_ok_preview()
    {
        await using var h = await Harness.CreateAsync(hasKey: true);
        var id = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(id, AppRoles.SuperAdmin);

        var result = await h.TestAsync();
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.True(result.Value.Ok);
        Assert.Equal("gpt-4o-mini", result.Value.Model);
        Assert.Equal("openai", result.Value.Provider);
        Assert.Equal("pong", result.Value.ReplyPreview);
        Assert.True(result.Value.LatencyMs >= 0);
        Assert.Equal(1, h.Client.CallCount);
        Assert.False(h.Client.LastRequest!.RequireJsonObjectResponse);
    }

    [Fact]
    public async Task Provider_failure_propagates()
    {
        await using var h = await Harness.CreateAsync(hasKey: true);
        h.Client.FailWith = DomainErrors.AiErrors.ServiceUnavailable;
        var id = await h.SeedUserAsync("super@subify.local", AppRoles.SuperAdmin);
        h.SetUser(id, AppRoles.SuperAdmin);

        var result = await h.TestAsync();
        Assert.Equal(DomainErrors.AiErrors.ServiceUnavailable.Code, result.Error.Code);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        public FakeAiClient Client { get; }
        public FakeAiSettingsResolver Resolver { get; }

        private Harness(
            SqliteConnection connection,
            ServiceProvider provider,
            FakeAiClient client,
            FakeAiSettingsResolver resolver)
        {
            _connection = connection;
            _provider = provider;
            Client = client;
            Resolver = resolver;
        }

        public static async Task<Harness> CreateAsync(bool hasKey = true)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var client = new FakeAiClient();
            var resolver = new FakeAiSettingsResolver { HasKey = hasKey };

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
            services.AddSingleton<IAiClient>(client);
            services.AddSingleton<IAiSettingsResolver>(resolver);
            services.AddScoped<ISubifyDbContext>(sp => sp.GetRequiredService<SubifyDbContext>());
            services.AddScoped<TestAiHandler>();

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
                    db.SystemSettings.Add(SystemSettings.CreateDefault());
                    await db.SaveChangesAsync();
                }
            }

            return new Harness(connection, provider, client, resolver);
        }

        public void SetUser(Guid userId, string role)
        {
            var fake = (FakeCurrentUser)_provider.GetRequiredService<ICurrentUserService>();
            fake.IsAuthenticated = true;
            fake.UserId = userId;
            fake.Email = "x@subify.local";
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

        public async Task<Result<TestAiResponse>> TestAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<TestAiHandler>()
                .Handle(new TestAiCommand(), CancellationToken.None);
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

        public sealed class FakeAiClient : IAiClient
        {
            public int CallCount { get; private set; }
            public AiChatCompletionRequest? LastRequest { get; private set; }
            public Error? FailWith { get; set; }

            public Task<Result<AiChatCompletionResult>> CompleteAsync(
                AiChatCompletionRequest request,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastRequest = request;
                if (FailWith is not null)
                {
                    return Task.FromResult(Result.Failure<AiChatCompletionResult>(FailWith));
                }

                return Task.FromResult(Result.Success(new AiChatCompletionResult(
                    Content: "pong",
                    Model: request.Model,
                    PromptTokens: 1,
                    CompletionTokens: 1)));
            }
        }

        public sealed class FakeAiSettingsResolver : IAiSettingsResolver
        {
            public bool HasKey { get; set; }

            public Task<Result<AiRuntimeSettings>> ResolveAsync(CancellationToken cancellationToken = default)
            {
                if (!HasKey)
                {
                    return Task.FromResult(Result.Failure<AiRuntimeSettings>(DomainErrors.AiErrors.ApiKeyMissing));
                }

                return Task.FromResult(Result.Success(new AiRuntimeSettings(
                    ApiKey: "sk-test",
                    Model: "gpt-4o-mini",
                    BaseUrl: "https://api.openai.com/v1",
                    Provider: "openai")));
            }
        }
    }
}
