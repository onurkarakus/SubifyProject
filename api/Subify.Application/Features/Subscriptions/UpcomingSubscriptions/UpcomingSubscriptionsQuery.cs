using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.UpcomingSubscriptions;

/// <summary>
/// Active subscriptions renewing within <see cref="Days"/> or already overdue (4.1.9 / 4.3.4).
/// </summary>
public sealed record UpcomingSubscriptionsQuery(
    int Days = SubscriptionConstants.DefaultUpcomingDays)
    : IRequest<Result<UpcomingSubscriptionsResponse>>;

public sealed record UpcomingSubscriptionItem(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    decimal UserShare,
    DateOnly NextRenewalDate,
    int DaysUntilRenewal,
    bool IsOverdue,
    bool IsUpcoming);

public sealed record UpcomingSubscriptionsResponse(
    IReadOnlyList<UpcomingSubscriptionItem> Data,
    decimal Total,
    string Currency,
    int Days,
    int OverdueCount,
    int UpcomingCount,
    IReadOnlyList<string> Warnings,
    bool HasUnconvertedAmounts);

public sealed class UpcomingSubscriptionsHandler
    : IRequestHandler<UpcomingSubscriptionsQuery, Result<UpcomingSubscriptionsResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateLookup _exchangeRates;

    public UpcomingSubscriptionsHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateLookup exchangeRates)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRates = exchangeRates;
    }

    public async Task<Result<UpcomingSubscriptionsResponse>> Handle(
        UpcomingSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<UpcomingSubscriptionsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var days = Math.Clamp(
            request.Days,
            SubscriptionConstants.MinUpcomingDays,
            SubscriptionConstants.MaxUpcomingDays);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowEnd = today.AddDays(days);

        var mainCurrency = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.MainCurrency)
            .FirstOrDefaultAsync(cancellationToken)
            ?? SupportedCurrencies.Default;

        // Active only: overdue (any past) OR renewal within [today, today+days]
        var entities = await _db.Subscriptions
            .AsNoTracking()
            .Where(s =>
                s.UserId == userId
                && !s.Archived
                && s.NextRenewalDate <= windowEnd)
            .OrderBy(s => s.NextRenewalDate)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var items = entities
            .Select(s =>
            {
                var daysUntil = s.DaysUntilRenewal(today);
                var isOverdue = daysUntil < 0;
                var isUpcoming = daysUntil >= 0 && daysUntil <= days;
                return new UpcomingSubscriptionItem(
                    Id: s.Id,
                    Name: s.Name,
                    Price: s.Price,
                    Currency: s.Currency,
                    UserShare: s.UserShare,
                    NextRenewalDate: s.NextRenewalDate,
                    DaysUntilRenewal: daysUntil,
                    IsOverdue: isOverdue,
                    IsUpcoming: isUpcoming);
            })
            .ToList();

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var currency = CurrencyConversion.Normalize(mainCurrency);
        decimal total = 0m;
        var warnings = new List<string>();
        var unconverted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var conv = CurrencyConversion.Convert(item.UserShare, item.Currency, currency, rates);
            if (conv.WasConverted)
            {
                total += conv.Amount;
            }
            else if (unconverted.Add(CurrencyConversion.Normalize(item.Currency)))
            {
                warnings.Add(
                    conv.Warning
                    ?? $"No exchange rate for {item.Currency}→{currency}; amounts excluded from total.");
            }
        }

        return Result.Success(new UpcomingSubscriptionsResponse(
            Data: items,
            Total: total,
            Currency: currency,
            Days: days,
            OverdueCount: items.Count(i => i.IsOverdue),
            UpcomingCount: items.Count(i => i.IsUpcoming),
            Warnings: warnings,
            HasUnconvertedAmounts: warnings.Count > 0));
    }
}
