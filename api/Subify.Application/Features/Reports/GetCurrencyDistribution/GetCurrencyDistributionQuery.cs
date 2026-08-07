using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Reports.GetCurrencyDistribution;

/// <summary>
/// Active subscription spend grouped by original currency (6.1.3).
/// MonthlyTotal is in the group currency; ConvertedMonthlyTotal / percentage use main currency.
/// </summary>
public sealed record GetCurrencyDistributionQuery(
    string? Currency = null) : IRequest<Result<CurrencyDistributionResponse>>;

public sealed class GetCurrencyDistributionHandler
    : IRequestHandler<GetCurrencyDistributionQuery, Result<CurrencyDistributionResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateLookup _exchangeRates;

    public GetCurrencyDistributionHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateLookup exchangeRates)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRates = exchangeRates;
    }

    public async Task<Result<CurrencyDistributionResponse>> Handle(
        GetCurrencyDistributionQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<CurrencyDistributionResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        var profileCurrency = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.MainCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        var mainCurrency = !string.IsNullOrWhiteSpace(request.Currency)
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
                s.Currency
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Result.Success(new CurrencyDistributionResponse(
                Data: Array.Empty<CurrencyDistributionItem>(),
                GrandTotal: 0m,
                Currency: mainCurrency,
                Message: DomainErrors.ReportErrors.InsufficientData.Description));
        }

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var buckets = new Dictionary<string, Bucket>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var code = CurrencyConversion.Normalize(row.Currency);
            if (!buckets.TryGetValue(code, out var bucket))
            {
                bucket = new Bucket(code);
                buckets[code] = bucket;
            }

            var local = ReportCalculation.LocalMonthly(row.Price, row.SharedWithCount, row.BillingCycle);
            bucket.MonthlyTotal += local;
            bucket.ConvertedMonthlyTotal += ReportCalculation.ConvertedMonthly(
                row.Price,
                row.SharedWithCount,
                row.BillingCycle,
                row.Currency,
                mainCurrency,
                rates);
            bucket.Count += 1;
        }

        var grandTotal = buckets.Values.Sum(b => b.ConvertedMonthlyTotal);

        var data = buckets.Values
            .OrderByDescending(b => b.ConvertedMonthlyTotal)
            .ThenBy(b => b.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(b => new CurrencyDistributionItem(
                Currency: b.Currency,
                MonthlyTotal: decimal.Round(b.MonthlyTotal, 2, MidpointRounding.AwayFromZero),
                ConvertedMonthlyTotal: decimal.Round(b.ConvertedMonthlyTotal, 2, MidpointRounding.AwayFromZero),
                Percentage: ReportCalculation.Percentage(b.ConvertedMonthlyTotal, grandTotal),
                Count: b.Count))
            .ToList();

        return Result.Success(new CurrencyDistributionResponse(
            Data: data,
            GrandTotal: decimal.Round(grandTotal, 2, MidpointRounding.AwayFromZero),
            Currency: mainCurrency,
            Message: null));
    }

    private sealed class Bucket(string currency)
    {
        public string Currency { get; } = currency;
        public decimal MonthlyTotal { get; set; }
        public decimal ConvertedMonthlyTotal { get; set; }
        public int Count { get; set; }
    }
}
