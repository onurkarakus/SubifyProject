using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Ai.AnalyzeSubscriptions;

/// <summary>
/// Analyze current user's active subscriptions via BYOK LLM (9.2.1 / 9.1.*).
/// Requires ≥1 active subscription; logs request/response; activity ai.analyze.
/// </summary>
public sealed record AnalyzeSubscriptionsCommand(
    string? Lang = null,
    string? AcceptLanguage = null) : IRequest<Result<AiAnalyzeResponse>>;

public sealed class AnalyzeSubscriptionsValidator : AbstractValidator<AnalyzeSubscriptionsCommand>
{
    public AnalyzeSubscriptionsValidator()
    {
        RuleFor(x => x.Lang!)
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language must be tr or en.")
            .When(x => !string.IsNullOrWhiteSpace(x.Lang));
    }
}

/// <summary>Bound from Ai:DailyLimit — kept on Application via options interface-less int from Infrastructure config.</summary>
public sealed class AiAnalyzeOptions
{
    public const string SectionName = "Ai";
    public int DailyLimit { get; set; } = 20;
    public double Temperature { get; set; } = 0.3;
}

public sealed class AnalyzeSubscriptionsHandler
    : IRequestHandler<AnalyzeSubscriptionsCommand, Result<AiAnalyzeResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly IAiClient _aiClient;
    private readonly IActivityLogger _activityLogger;
    private readonly AiAnalyzeOptions _aiOptions;

    public AnalyzeSubscriptionsHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IAiSettingsResolver settingsResolver,
        IAiClient aiClient,
        IActivityLogger activityLogger,
        IOptions<AiAnalyzeOptions> aiOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _settingsResolver = settingsResolver;
        _aiClient = aiClient;
        _activityLogger = activityLogger;
        _aiOptions = aiOptions.Value;
    }

    public async Task<Result<AiAnalyzeResponse>> Handle(
        AnalyzeSubscriptionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<AiAnalyzeResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var locale = LocaleResolver.Resolve(request.Lang, request.AcceptLanguage, _currentUser);

        // 9.2.4 daily cap (minute cap = ASP.NET Ai policy).
        // Materialize for SQLite DateTimeOffset safety.
        var dayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var createdAts = await _db.AISuggestionLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        var usedToday = createdAts.Count(c => c >= dayStart);
        var dailyLimit = Math.Max(1, _aiOptions.DailyLimit);
        if (usedToday >= dailyLimit)
        {
            return Result.Failure<AiAnalyzeResponse>(DomainErrors.AiErrors.RateLimitExceededDaily);
        }

        var settings = await _settingsResolver.ResolveAsync(cancellationToken);
        if (settings.IsFailure)
        {
            return Result.Failure<AiAnalyzeResponse>(settings.Error);
        }

        var runtime = settings.Value;

        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.MainCurrency, u.Locale })
            .FirstOrDefaultAsync(cancellationToken);

        var mainCurrency = SupportedCurrencies.Normalize(profile?.MainCurrency);
        if (string.IsNullOrWhiteSpace(request.Lang) && string.IsNullOrWhiteSpace(request.AcceptLanguage)
            && !string.IsNullOrWhiteSpace(profile?.Locale))
        {
            locale = SupportedLocales.Normalize(profile.Locale);
        }

        var subscriptions = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.Archived && s.DeletedAt == null)
            .Include(s => s.Category)
            .Include(s => s.UserCategory)
            .ToListAsync(cancellationToken);

        // 9.2.3
        if (subscriptions.Count == 0)
        {
            return Result.Failure<AiAnalyzeResponse>(DomainErrors.AiErrors.InsufficientData);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var systemPrompt = AiPromptBuilder.BuildSystemPrompt(locale);
        var userPrompt = AiPromptBuilder.BuildUserPrompt(subscriptions, mainCurrency, locale, today);

        var completion = await _aiClient.CompleteAsync(
            new AiChatCompletionRequest(
                ApiKey: runtime.ApiKey,
                Model: runtime.Model,
                BaseUrl: runtime.BaseUrl,
                Messages:
                [
                    new AiChatMessage("system", systemPrompt),
                    new AiChatMessage("user", userPrompt)
                ],
                Temperature: _aiOptions.Temperature,
                RequireJsonObjectResponse: true),
            cancellationToken);

        if (completion.IsFailure)
        {
            return Result.Failure<AiAnalyzeResponse>(completion.Error);
        }

        var analyzedAt = DateTimeOffset.UtcNow;
        var parsed = AiResponseParser.Parse(completion.Value.Content, analyzedAt);
        if (parsed.IsFailure)
        {
            return Result.Failure<AiAnalyzeResponse>(parsed.Error);
        }

        var response = parsed.Value;
        var requestLog = AiPromptBuilder.BuildRequestLogPayload(
            locale, mainCurrency, subscriptions.Count, runtime.Model, runtime.Provider);
        var responseLog = JsonSerializer.Serialize(response, JsonOptions);

        // 9.2.5 persist
        _db.AISuggestionLogs.Add(AiSuggestionLog.Create(userId, requestLog, responseLog));

        // 9.2.6 activity
        await _activityLogger.LogAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.AiSuggestion,
            action: ActivityLogConstants.Actions.AiAnalyze,
            description: "AI subscription analysis completed.",
            entityId: null,
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                tipCount = response.Tips.Count,
                estimatedMonthlySaving = response.EstimatedMonthlySaving,
                model = runtime.Model
            }, JsonOptions),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(response);
    }
}
