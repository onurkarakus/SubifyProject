using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Activity;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Ai;
using Subify.Application.Features.Ai.AnalyzeSubscriptions;
using Subify.Application.Features.Ai.GetAiHistory;
using Subify.Application.Features.Ai.GetAiHistoryById;
using Subify.Application.Features.Ai.ReportCommentary;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.ExchangeRates;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Tests;

/// <summary>Faz 9 — AI analyze, history, key missing, insufficient data, parse, daily limit.</summary>
public class AiHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Analyze_requires_api_key()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: false);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);

        var result = await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand(Lang: "en"));
        Assert.Equal(DomainErrors.AiErrors.ApiKeyMissing.Code, result.Error.Code);
    }

    [Fact]
    public async Task Analyze_requires_at_least_one_subscription()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand(Lang: "en"));
        Assert.Equal(DomainErrors.AiErrors.InsufficientData.Code, result.Error.Code);
    }

    [Fact]
    public async Task Analyze_success_persists_log_activity_and_history()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);
        await harness.CreateSubAsync(userId, "Spotify", 50m);

        harness.AiClient.NextContent = """
            {
              "summary": "You spend about 150 TRY monthly.",
              "tips": [
                {
                  "type": "unused",
                  "message": "Review Netflix if unused.",
                  "potentialSaving": 100,
                  "subscriptionName": "Netflix"
                },
                {
                  "type": "general",
                  "message": "Consider yearly plans.",
                  "potentialSaving": 20
                }
              ],
              "estimatedMonthlySaving": 120,
              "estimatedYearlySaving": 1440
            }
            """;

        var result = await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand(Lang: "en"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Contains("150", result.Value.Summary);
        Assert.Equal(2, result.Value.Tips.Count);
        Assert.Equal(120m, result.Value.EstimatedMonthlySaving);
        Assert.Equal(1, harness.AiClient.CallCount);

        var history = await harness.HistoryAsync(new GetAiHistoryQuery(Page: 1, PageSize: 10));
        Assert.True(history.IsSuccess);
        Assert.Single(history.Value.Data);
        Assert.Equal(120m, history.Value.Data[0].EstimatedMonthlySaving);

        var detail = await harness.HistoryByIdAsync(
            new GetAiHistoryByIdQuery(history.Value.Data[0].Id));
        Assert.True(detail.IsSuccess, detail.IsFailure ? detail.Error.Code : null);
        Assert.Equal(2, detail.Value.Tips.Count);
        Assert.Contains("150", detail.Value.Summary);
        Assert.Equal(120m, detail.Value.EstimatedMonthlySaving);

        var activities = await harness.GetActivityAsync(userId);
        Assert.Contains(activities, a => a.Action == ActivityLogConstants.Actions.AiAnalyze);
    }

    [Fact]
    public async Task HistoryById_other_user_not_found()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        var a = await harness.SeedUserAsync("a@subify.local");
        var b = await harness.SeedUserAsync("b@subify.local");
        harness.SetUser(a);
        await harness.CreateSubAsync(a, "A Sub", 10m);
        harness.AiClient.NextContent =
            """{"summary":"A only","tips":[{"type":"general","message":"Tip A"}],"estimatedMonthlySaving":1,"estimatedYearlySaving":12}""";
        Assert.True((await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand())).IsSuccess);
        var history = await harness.HistoryAsync(new GetAiHistoryQuery());
        var id = history.Value.Data[0].Id;

        harness.SetUser(b);
        var detail = await harness.HistoryByIdAsync(new GetAiHistoryByIdQuery(id));
        Assert.Equal(DomainErrors.AiErrors.HistoryNotFound.Code, detail.Error.Code);
    }

    [Fact]
    public async Task Analyze_rejects_unparseable_ai_payload()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);
        harness.AiClient.NextContent = "not-json-at-all";

        var result = await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand(Lang: "en"));
        Assert.Equal(DomainErrors.AiErrors.ProcessingError.Code, result.Error.Code);
    }

    [Fact]
    public async Task Analyze_daily_limit_enforced()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true, dailyLimit: 2);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);

        harness.AiClient.NextContent = """
            {"summary":"ok","tips":[],"estimatedMonthlySaving":0,"estimatedYearlySaving":0}
            """;

        Assert.True((await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand())).IsSuccess);
        Assert.True((await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand())).IsSuccess);
        var third = await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand());
        Assert.Equal(DomainErrors.AiErrors.RateLimitExceededDaily.Code, third.Error.Code);
    }

    [Fact]
    public async Task History_is_user_scoped()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        var a = await harness.SeedUserAsync("a@subify.local");
        var b = await harness.SeedUserAsync("b@subify.local");
        harness.SetUser(a);
        await harness.CreateSubAsync(a, "A Sub", 10m);
        harness.AiClient.NextContent =
            """{"summary":"A only","tips":[],"estimatedMonthlySaving":1,"estimatedYearlySaving":12}""";
        Assert.True((await harness.AnalyzeAsync(new AnalyzeSubscriptionsCommand())).IsSuccess);

        harness.SetUser(b);
        var history = await harness.HistoryAsync(new GetAiHistoryQuery());
        Assert.Empty(history.Value.Data);
    }

    [Fact]
    public void AiResponseParser_strips_markdown_fence()
    {
        var content = """
            ```json
            {"summary":"Hi","tips":[{"type":"general","message":"Tip"}],"estimatedMonthlySaving":5,"estimatedYearlySaving":60}
            ```
            """;
        var parsed = AiResponseParser.Parse(content, DateTimeOffset.UtcNow);
        Assert.True(parsed.IsSuccess);
        Assert.Equal("Hi", parsed.Value.Summary);
        Assert.Equal(AiTipTypes.General, parsed.Value.Tips[0].Type);
    }

    [Fact]
    public async Task ReportCommentary_requires_api_key()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: false);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);

        var result = await harness.ReportCommentaryAsync(new ReportCommentaryCommand(Months: 6, Lang: "en"));
        Assert.Equal(DomainErrors.AiErrors.ApiKeyMissing.Code, result.Error.Code);
    }

    [Fact]
    public async Task ReportCommentary_requires_subscription_history()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        harness.SetUser(await harness.SeedUserAsync("u@subify.local"));

        var result = await harness.ReportCommentaryAsync(new ReportCommentaryCommand(Months: 6, Lang: "en"));
        Assert.Equal(DomainErrors.AiErrors.InsufficientData.Code, result.Error.Code);
    }

    [Fact]
    public async Task ReportCommentary_success_persists_log_and_activity()
    {
        await using var harness = await Harness.CreateAsync(withAiKey: true);
        var userId = await harness.SeedUserAsync("u@subify.local");
        harness.SetUser(userId);
        await harness.CreateSubAsync(userId, "Netflix", 100m);
        await harness.CreateSubAsync(userId, "Spotify", 50m);

        harness.AiClient.NextContent = """
            {
              "summary": "Your spend is steady around 150 TRY.",
              "highlights": [
                "Entertainment is the top category.",
                "Month-over-month change is small."
              ],
              "trend": "stable",
              "budgetNote": null
            }
            """;

        var result = await harness.ReportCommentaryAsync(
            new ReportCommentaryCommand(Months: 6, Lang: "en"));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        Assert.Contains("150", result.Value.Summary);
        Assert.Equal(AiReportTrends.Stable, result.Value.Trend);
        Assert.Equal(2, result.Value.Highlights.Count);
        Assert.Equal(6, result.Value.Months);
        Assert.Equal(1, harness.AiClient.CallCount);

        var history = await harness.HistoryAsync(new GetAiHistoryQuery(Page: 1, PageSize: 10));
        Assert.True(history.IsSuccess);
        Assert.Single(history.Value.Data);
        Assert.Contains("150", history.Value.Data[0].Summary);

        var activities = await harness.GetActivityAsync(userId);
        Assert.Contains(activities, a => a.Action == ActivityLogConstants.Actions.AiReportCommentary);
    }

    [Fact]
    public void AiResponseParser_report_commentary_normalizes_trend()
    {
        var parsed = AiResponseParser.ParseReportCommentary(
            """{"summary":"Ok","highlights":["A"],"trend":"UP","budgetNote":"Near limit"}""",
            months: 3,
            currency: "TRY",
            generatedAt: DateTimeOffset.UtcNow);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(AiReportTrends.Up, parsed.Value.Trend);
        Assert.Equal("Near limit", parsed.Value.BudgetNote);
        Assert.Equal(3, parsed.Value.Months);
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

        public FakeAiClient AiClient => _provider.GetRequiredService<FakeAiClient>();

        public static async Task<Harness> CreateAsync(bool withAiKey, int dailyLimit = 20)
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
            services.AddScoped<IExchangeRateLookup, ExchangeRateLookup>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddSingleton<FakeAiClient>();
            services.AddSingleton<IAiClient>(sp => sp.GetRequiredService<FakeAiClient>());
            services.AddSingleton<FakeAiSettingsResolver>();
            services.AddSingleton<IAiSettingsResolver>(sp => sp.GetRequiredService<FakeAiSettingsResolver>());
            services.Configure<AiAnalyzeOptions>(o =>
            {
                o.DailyLimit = dailyLimit;
                o.Temperature = 0.2;
            });
            services.AddScoped<AnalyzeSubscriptionsHandler>();
            services.AddScoped<ReportCommentaryHandler>();
            services.AddScoped<GetAiHistoryHandler>();
            services.AddScoped<GetAiHistoryByIdHandler>();
            services.AddScoped<CreateSubscriptionHandler>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();
            provider.GetRequiredService<FakeAiSettingsResolver>().HasKey = withAiKey;

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
                if (withAiKey)
                {
                    settings.UpdateAi(aiProvider: "openai", aiApiKey: "sk-test", aiModel: "gpt-4o-mini");
                }

                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();
            }

            return new Harness(connection, provider);
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
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(",", created.Errors.Select(e => e.Code)));
            }

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

        public async Task<Result<AiAnalyzeResponse>> AnalyzeAsync(AnalyzeSubscriptionsCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<AnalyzeSubscriptionsHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<AiReportCommentaryResponse>> ReportCommentaryAsync(
            ReportCommentaryCommand command)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ReportCommentaryHandler>()
                .Handle(command, CancellationToken.None);
        }

        public async Task<Result<ListAiHistoryResponse>> HistoryAsync(GetAiHistoryQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetAiHistoryHandler>()
                .Handle(query, CancellationToken.None);
        }

        public async Task<Result<AiHistoryDetailResponse>> HistoryByIdAsync(GetAiHistoryByIdQuery query)
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GetAiHistoryByIdHandler>()
                .Handle(query, CancellationToken.None);
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

    public sealed class FakeAiClient : IAiClient
    {
        public string NextContent { get; set; } = """{"summary":"ok","tips":[],"estimatedMonthlySaving":0,"estimatedYearlySaving":0}""";
        public int CallCount { get; private set; }

        public Task<Result<AiChatCompletionResult>> CompleteAsync(
            AiChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result.Success(new AiChatCompletionResult(
                Content: NextContent,
                Model: request.Model,
                PromptTokens: 10,
                CompletionTokens: 20)));
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
