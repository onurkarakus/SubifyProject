using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Application.Features.Ai.AnalyzeSubscriptions;
using Subify.Application.Features.Reports;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Ai.ReportCommentary;

/// <summary>
/// Period report commentary via BYOK LLM — monthly series + categories + optional budget.
/// Shares daily AI cap and suggestion log with analyze.
/// </summary>
public sealed record ReportCommentaryCommand(
    int Months = 6,
    string? Lang = null,
    string? AcceptLanguage = null) : IRequest<Result<AiReportCommentaryResponse>>;

public sealed class ReportCommentaryValidator : AbstractValidator<ReportCommentaryCommand>
{
    public ReportCommentaryValidator()
    {
        RuleFor(x => x.Months).InclusiveBetween(3, 12);
        RuleFor(x => x.Lang!)
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language must be tr or en.")
            .When(x => !string.IsNullOrWhiteSpace(x.Lang));
    }
}

public sealed class ReportCommentaryHandler
    : IRequestHandler<ReportCommentaryCommand, Result<AiReportCommentaryResponse>>
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
    private readonly IExchangeRateLookup _exchangeRates;
    private readonly AiAnalyzeOptions _aiOptions;

    public ReportCommentaryHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IAiSettingsResolver settingsResolver,
        IAiClient aiClient,
        IActivityLogger activityLogger,
        IExchangeRateLookup exchangeRates,
        IOptions<AiAnalyzeOptions> aiOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _settingsResolver = settingsResolver;
        _aiClient = aiClient;
        _activityLogger = activityLogger;
        _exchangeRates = exchangeRates;
        _aiOptions = aiOptions.Value;
    }

    public async Task<Result<AiReportCommentaryResponse>> Handle(
        ReportCommentaryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<AiReportCommentaryResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var locale = LocaleResolver.Resolve(request.Lang, request.AcceptLanguage, _currentUser);
        var months = Math.Clamp(request.Months, 3, 12);

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
            return Result.Failure<AiReportCommentaryResponse>(DomainErrors.AiErrors.RateLimitExceededDaily);
        }

        var settings = await _settingsResolver.ResolveAsync(cancellationToken);
        if (settings.IsFailure)
        {
            return Result.Failure<AiReportCommentaryResponse>(settings.Error);
        }

        var runtime = settings.Value;

        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.MainCurrency, u.Locale, u.MonthlyBudget })
            .FirstOrDefaultAsync(cancellationToken);

        var mainCurrency = SupportedCurrencies.Normalize(profile?.MainCurrency);
        if (string.IsNullOrWhiteSpace(request.Lang) && string.IsNullOrWhiteSpace(request.AcceptLanguage)
            && !string.IsNullOrWhiteSpace(profile?.Locale))
        {
            locale = SupportedLocales.Normalize(profile.Locale);
        }

        var historyRows = await _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.Price,
                s.SharedWithCount,
                s.BillingCycle,
                s.Currency,
                s.CreatedAt,
                ArchivedAt = s.DeletedAt
            })
            .ToListAsync(cancellationToken);

        if (historyRows.Count == 0)
        {
            return Result.Failure<AiReportCommentaryResponse>(DomainErrors.AiErrors.InsufficientData);
        }

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var windows = ReportCalculation.BuildMonthWindows(months, DateTimeOffset.UtcNow);
        var series = windows
            .Select(w =>
            {
                var total = historyRows
                    .Where(r => ReportCalculation.WasActiveDuring(
                        r.CreatedAt, r.ArchivedAt, w.Start, w.EndExclusive))
                    .Sum(r => ReportCalculation.ConvertedMonthly(
                        r.Price, r.SharedWithCount, r.BillingCycle, r.Currency, mainCurrency, rates));
                return (Month: w.Key, Total: decimal.Round(total, 2, MidpointRounding.AwayFromZero));
            })
            .ToList();

        var average = series.Count > 0
            ? decimal.Round(series.Average(p => p.Total), 2, MidpointRounding.AwayFromZero)
            : 0m;
        var latest = series.Count > 0 ? series[^1].Total : 0m;
        decimal? momChangePct = null;
        if (series.Count >= 2 && series[^2].Total > 0)
        {
            momChangePct = decimal.Round(
                (series[^1].Total - series[^2].Total) / series[^2].Total * 100m,
                1,
                MidpointRounding.AwayFromZero);
        }

        var activeRows = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.Archived && s.DeletedAt == null)
            .Select(s => new
            {
                s.Price,
                s.SharedWithCount,
                s.BillingCycle,
                s.Currency,
                SystemSlug = s.Category != null ? s.Category.Slug : null,
                UserName = s.UserCategory != null ? s.UserCategory.Name : null
            })
            .ToListAsync(cancellationToken);

        var activeCount = activeRows.Count;
        var categoryGroups = activeRows
            .GroupBy(r =>
            {
                if (!string.IsNullOrWhiteSpace(r.SystemSlug)) return r.SystemSlug!;
                if (!string.IsNullOrWhiteSpace(r.UserName)) return r.UserName!;
                return ReportConstants.UncategorizedKey;
            })
            .Select(g =>
            {
                var total = g.Sum(r => ReportCalculation.ConvertedMonthly(
                    r.Price, r.SharedWithCount, r.BillingCycle, r.Currency, mainCurrency, rates));
                return new { Category = g.Key, Total = total, Count = g.Count() };
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var catGrand = categoryGroups.Sum(x => x.Total);
        var topCategories = categoryGroups
            .Take(6)
            .Select(x => (
                Category: x.Category,
                Total: decimal.Round(x.Total, 2, MidpointRounding.AwayFromZero),
                Percentage: ReportCalculation.Percentage(x.Total, catGrand),
                Count: x.Count))
            .ToList();

        var monthlyBudget = profile?.MonthlyBudget is > 0 ? profile.MonthlyBudget : null;

        var systemPrompt = AiPromptBuilder.BuildReportCommentarySystemPrompt(locale);
        var userPrompt = AiPromptBuilder.BuildReportCommentaryUserPrompt(
            months,
            mainCurrency,
            locale,
            monthlyBudget,
            average,
            latest,
            momChangePct,
            series,
            topCategories,
            activeCount);

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
            return Result.Failure<AiReportCommentaryResponse>(completion.Error);
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var parsed = AiResponseParser.ParseReportCommentary(
            completion.Value.Content, months, mainCurrency, generatedAt);
        if (parsed.IsFailure)
        {
            return Result.Failure<AiReportCommentaryResponse>(parsed.Error);
        }

        var response = parsed.Value;

        // History-compatible payload so /ai/history list/detail still work.
        var historyPayload = new AiAnalyzeResponse(
            Summary: response.Summary,
            Tips: response.Highlights
                .Select(h => new AiTipDto(AiTipTypes.General, h))
                .ToList(),
            EstimatedMonthlySaving: 0m,
            EstimatedYearlySaving: 0m,
            AnalyzedAt: generatedAt);

        var requestLog = AiPromptBuilder.BuildReportCommentaryRequestLogPayload(
            locale, mainCurrency, months, activeCount, runtime.Model, runtime.Provider);
        var responseLog = JsonSerializer.Serialize(new
        {
            historyPayload.Summary,
            historyPayload.Tips,
            historyPayload.EstimatedMonthlySaving,
            historyPayload.EstimatedYearlySaving,
            historyPayload.AnalyzedAt,
            kind = "report-commentary",
            response.Trend,
            response.BudgetNote,
            response.Months,
            response.Currency
        }, JsonOptions);

        _db.AISuggestionLogs.Add(AiSuggestionLog.Create(userId, requestLog, responseLog));

        await _activityLogger.LogAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.AiSuggestion,
            action: ActivityLogConstants.Actions.AiReportCommentary,
            description: "AI report period commentary completed.",
            entityId: null,
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                months,
                trend = response.Trend,
                highlightCount = response.Highlights.Count,
                model = runtime.Model
            }, JsonOptions),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(response);
    }
}
