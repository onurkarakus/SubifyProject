using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Localization;
using Subify.Application.Features.Categories;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Reports.GetCategoryBreakdown;

/// <summary>
/// Active subscription spend by category (6.1.2): total, percentage, count, color.
/// System category names localized; user categories use stored name.
/// </summary>
public sealed record GetCategoryBreakdownQuery(
    string? AcceptLanguage = null,
    string? ExplicitLocale = null,
    string? Currency = null) : IRequest<Result<CategoryBreakdownResponse>>;

public sealed class GetCategoryBreakdownHandler
    : IRequestHandler<GetCategoryBreakdownQuery, Result<CategoryBreakdownResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateLookup _exchangeRates;
    private readonly ICategoryNameLookup _names;

    public GetCategoryBreakdownHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateLookup exchangeRates,
        ICategoryNameLookup names)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRates = exchangeRates;
        _names = names;
    }

    public async Task<Result<CategoryBreakdownResponse>> Handle(
        GetCategoryBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<CategoryBreakdownResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        var profileCurrency = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.MainCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        var currency = !string.IsNullOrWhiteSpace(request.Currency)
            ? SupportedCurrencies.Normalize(request.Currency)
            : SupportedCurrencies.Normalize(profileCurrency);

        var rows = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.Archived && s.DeletedAt == null)
            .Select(s => new
            {
                s.Price,
                s.SharedWithCount,
                s.BillingCycle,
                s.Currency,
                s.CategoryId,
                s.UserCategoryId,
                SystemSlug = s.Category != null ? s.Category.Slug : null,
                SystemColor = s.Category != null ? s.Category.Color : null,
                UserName = s.UserCategory != null ? s.UserCategory.Name : null,
                UserColor = s.UserCategory != null ? s.UserCategory.Color : null
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Result.Success(new CategoryBreakdownResponse(
                Data: Array.Empty<CategoryBreakdownItem>(),
                GrandTotal: 0m,
                Currency: currency,
                Message: DomainErrors.ReportErrors.InsufficientData.Description));
        }

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var locale = LocaleResolver.Resolve(request.ExplicitLocale, request.AcceptLanguage, _currentUser);
        var nameMap = await _names.GetNamesAsync(locale, cancellationToken);

        var buckets = new Dictionary<string, Bucket>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var amount = ReportCalculation.ConvertedMonthly(
                row.Price,
                row.SharedWithCount,
                row.BillingCycle,
                row.Currency,
                currency,
                rates);

            string key;
            string name;
            string? color;

            if (row.CategoryId is not null && !string.IsNullOrWhiteSpace(row.SystemSlug))
            {
                key = row.SystemSlug!;
                name = _names.ResolveName(key, nameMap);
                color = row.SystemColor;
            }
            else if (row.UserCategoryId is { } userCatId)
            {
                key = $"user:{userCatId:N}";
                name = string.IsNullOrWhiteSpace(row.UserName) ? "Custom" : row.UserName!;
                color = row.UserColor;
            }
            else
            {
                key = ReportConstants.UncategorizedKey;
                name = ReportConstants.UncategorizedName;
                color = ReportConstants.UncategorizedColor;
            }

            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new Bucket(key, name, color);
                buckets[key] = bucket;
            }

            bucket.Total += amount;
            bucket.Count += 1;
        }

        var grandTotal = buckets.Values.Sum(b => b.Total);

        var data = buckets.Values
            .OrderByDescending(b => b.Total)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => new CategoryBreakdownItem(
                Category: b.Key,
                Name: b.Name,
                Color: b.Color,
                Total: decimal.Round(b.Total, 2, MidpointRounding.AwayFromZero),
                Percentage: ReportCalculation.Percentage(b.Total, grandTotal),
                Count: b.Count))
            .ToList();

        return Result.Success(new CategoryBreakdownResponse(
            Data: data,
            GrandTotal: decimal.Round(grandTotal, 2, MidpointRounding.AwayFromZero),
            Currency: currency,
            Message: null));
    }

    private sealed class Bucket(string key, string name, string? color)
    {
        public string Key { get; } = key;
        public string Name { get; } = name;
        public string? Color { get; } = color;
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}
