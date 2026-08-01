using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Application.Common.Options;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Reports.SendReportSummary;

/// <summary>
/// Emails the current user a period spend summary (SMTP).
/// Uses monthly series + category snapshot + optional budget.
/// </summary>
public sealed record SendReportSummaryCommand(
    int Months = 6,
    string? Lang = null,
    string? AcceptLanguage = null) : IRequest<Result<SendReportSummaryResponse>>;

public sealed record SendReportSummaryResponse(
    string ToEmail,
    int Months,
    string Currency,
    DateTimeOffset SentAt);

public sealed class SendReportSummaryValidator : AbstractValidator<SendReportSummaryCommand>
{
    public SendReportSummaryValidator()
    {
        RuleFor(x => x.Months).Must(m => m is 3 or 6 or 12)
            .WithMessage("Months must be 3, 6, or 12.");
        RuleFor(x => x.Lang!)
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language must be tr or en.")
            .When(x => !string.IsNullOrWhiteSpace(x.Lang));
    }
}

public sealed class SendReportSummaryHandler
    : IRequestHandler<SendReportSummaryCommand, Result<SendReportSummaryResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailSender _emailSender;
    private readonly IEmailDeliveryService _delivery;
    private readonly IExchangeRateLookup _exchangeRates;
    private readonly IActivityLogger _activityLogger;
    private readonly AppOptions _app;

    public SendReportSummaryHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IEmailSender emailSender,
        IEmailDeliveryService delivery,
        IExchangeRateLookup exchangeRates,
        IActivityLogger activityLogger,
        IOptions<AppOptions> app)
    {
        _db = db;
        _currentUser = currentUser;
        _emailSender = emailSender;
        _delivery = delivery;
        _exchangeRates = exchangeRates;
        _activityLogger = activityLogger;
        _app = app.Value;
    }

    public async Task<Result<SendReportSummaryResponse>> Handle(
        SendReportSummaryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SendReportSummaryResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var months = request.Months is 3 or 12 ? request.Months : 6;

        if (!await _emailSender.IsConfiguredAsync(cancellationToken))
        {
            return Result.Failure<SendReportSummaryResponse>(
                DomainErrors.SystemSettingsErrors.SmtpNotConfigured);
        }

        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Email,
                u.FullName,
                u.MainCurrency,
                u.Locale,
                u.MonthlyBudget
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
        {
            return Result.Failure<SendReportSummaryResponse>(DomainErrors.ProfileErrors.ProfileNotFound);
        }

        var locale = LocaleResolver.Resolve(request.Lang, request.AcceptLanguage, _currentUser);
        if (string.IsNullOrWhiteSpace(request.Lang) && string.IsNullOrWhiteSpace(request.AcceptLanguage)
            && !string.IsNullOrWhiteSpace(profile.Locale))
        {
            locale = SupportedLocales.Normalize(profile.Locale);
        }

        var mainCurrency = SupportedCurrencies.Normalize(profile.MainCurrency);

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
            return Result.Failure<SendReportSummaryResponse>(DomainErrors.ReportErrors.InsufficientData);
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
            .Take(8)
            .ToList();

        var catGrand = categoryGroups.Sum(x => x.Total);
        var culture = CultureInfo.InvariantCulture;
        var seriesHtml = BuildListHtml(
            series.Select(p => $"{Escape(p.Month)}: {p.Total.ToString("0.##", culture)} {Escape(mainCurrency)}"));
        var categoriesHtml = categoryGroups.Count == 0
            ? (locale == SupportedLocales.En ? "<p style=\"color:#888;\">No active categories.</p>" : "<p style=\"color:#888;\">Aktif kategori yok.</p>")
            : BuildListHtml(categoryGroups.Select(c =>
            {
                var pct = ReportCalculation.Percentage(c.Total, catGrand);
                return $"{Escape(c.Category)} — {c.Total.ToString("0.##", culture)} {Escape(mainCurrency)} ({pct.ToString("0.#", culture)}%, {c.Count})";
            }));

        var budgetLine = BuildBudgetLine(profile.MonthlyBudget, latest, mainCurrency, locale, culture);
        var fullName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.Email
            : profile.FullName.Trim();
        var sentAt = DateTimeOffset.UtcNow;

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullName"] = fullName,
            ["Months"] = months.ToString(culture),
            ["Currency"] = mainCurrency,
            ["AverageMonthly"] = average.ToString("0.##", culture),
            ["LatestMonth"] = latest.ToString("0.##", culture),
            ["ActiveCount"] = activeRows.Count.ToString(culture),
            ["BudgetLine"] = budgetLine,
            ["SeriesHtml"] = seriesHtml,
            ["CategoriesHtml"] = categoriesHtml,
            ["GeneratedAt"] = sentAt.ToString("u", culture),
            ["AppUrl"] = _app.BaseUrl
        };

        // One successful send per user/day/months (dedupe); user can still re-try if SMTP failed.
        var day = sentAt.UtcDateTime.ToString("yyyy-MM-dd", culture);
        var dedupeKey = $"report-summary:{userId:N}:{day}:{months}";

        var mail = await _delivery.SendTemplatedAsync(
            templateName: SystemEmailTemplates.Names.ReportSummary,
            locale: locale,
            toEmail: profile.Email,
            tokens: tokens,
            userId: userId,
            relatedEntityId: null,
            dedupeKey: dedupeKey,
            cancellationToken: cancellationToken);

        if (mail.IsFailure)
        {
            return Result.Failure<SendReportSummaryResponse>(mail.Error);
        }

        await _activityLogger.LogAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.Profile,
            action: ActivityLogConstants.Actions.ReportEmailSummary,
            description: "Period report summary emailed.",
            entityId: null,
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                months,
                currency = mainCurrency,
                toEmail = profile.Email,
                average,
                latest
            }, JsonOptions),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new SendReportSummaryResponse(
            ToEmail: profile.Email,
            Months: months,
            Currency: mainCurrency,
            SentAt: sentAt));
    }

    private static string BuildBudgetLine(
        decimal? monthlyBudget,
        decimal latest,
        string currency,
        string locale,
        CultureInfo culture)
    {
        if (monthlyBudget is not > 0)
        {
            return locale == SupportedLocales.En ? "Not set" : "Tanımlı değil";
        }

        var budget = monthlyBudget.Value;
        var util = budget > 0
            ? decimal.Round(latest / budget * 100m, 0, MidpointRounding.AwayFromZero)
            : 0m;
        var en = locale == SupportedLocales.En;
        return en
            ? $"{budget.ToString("0.##", culture)} {currency} ({util}% of latest month)"
            : $"{budget.ToString("0.##", culture)} {currency} (son aya göre %{util})";
    }

    private static string BuildListHtml(IEnumerable<string> items)
    {
        var sb = new StringBuilder();
        sb.Append("<ul style=\"padding-left:18px;margin:8px 0;\">");
        var any = false;
        foreach (var item in items)
        {
            any = true;
            sb.Append("<li>").Append(item).Append("</li>");
        }

        if (!any)
        {
            sb.Append("<li>—</li>");
        }

        sb.Append("</ul>");
        return sb.ToString();
    }

    private static string Escape(string value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
