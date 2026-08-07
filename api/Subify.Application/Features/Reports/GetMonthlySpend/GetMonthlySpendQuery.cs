using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Reports.GetMonthlySpend;

/// <summary>
/// Monthly spend chart for last N months (6.1.1). No premium gate.
/// Totals use user-share monthly equivalents converted to target currency (FX snapshot).
/// Historical: includes subs active during each calendar month (CreatedAt / archive).
/// </summary>
public sealed record GetMonthlySpendQuery(
    int Months = ReportConstants.DefaultMonths,
    string? Currency = null) : IRequest<Result<MonthlySpendResponse>>;

public sealed class GetMonthlySpendValidator : AbstractValidator<GetMonthlySpendQuery>
{
    public GetMonthlySpendValidator()
    {
        RuleFor(x => x.Months)
            .InclusiveBetween(ReportConstants.MinMonths, ReportConstants.MaxMonths);

        RuleFor(x => x.Currency!)
            .Must(SupportedCurrencies.IsSupported)
            .WithMessage("Currency is not supported.")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }
}

public sealed class GetMonthlySpendHandler
    : IRequestHandler<GetMonthlySpendQuery, Result<MonthlySpendResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateLookup _exchangeRates;

    public GetMonthlySpendHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateLookup exchangeRates)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRates = exchangeRates;
    }

    public async Task<Result<MonthlySpendResponse>> Handle(
        GetMonthlySpendQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<MonthlySpendResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var months = Math.Clamp(request.Months, ReportConstants.MinMonths, ReportConstants.MaxMonths);

        var profileCurrency = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.MainCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        var currency = !string.IsNullOrWhiteSpace(request.Currency)
            ? SupportedCurrencies.Normalize(request.Currency)
            : SupportedCurrencies.Normalize(profileCurrency);

        // History needs archived rows too.
        var rows = await _db.Subscriptions
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

        if (rows.Count == 0)
        {
            return Result.Success(new MonthlySpendResponse(
                Data: Array.Empty<MonthlySpendPoint>(),
                Currency: currency,
                Average: 0m,
                Message: DomainErrors.ReportErrors.InsufficientData.Description));
        }

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var windows = ReportCalculation.BuildMonthWindows(months, DateTimeOffset.UtcNow);

        var points = new List<MonthlySpendPoint>(windows.Count);
        foreach (var (key, start, end) in windows)
        {
            decimal total = 0m;
            foreach (var row in rows)
            {
                if (!ReportCalculation.WasActiveDuring(row.CreatedAt, row.ArchivedAt, start, end))
                {
                    continue;
                }

                total += ReportCalculation.ConvertedMonthly(
                    row.Price,
                    row.SharedWithCount,
                    row.BillingCycle,
                    row.Currency,
                    currency,
                    rates);
            }

            points.Add(new MonthlySpendPoint(key, total));
        }

        var average = points.Count == 0
            ? 0m
            : decimal.Round(points.Average(p => p.Total), 2, MidpointRounding.AwayFromZero);

        return Result.Success(new MonthlySpendResponse(
            Data: points,
            Currency: currency,
            Average: average,
            Message: null));
    }
}
