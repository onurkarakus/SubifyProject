using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Application.Features.ExchangeRates.GetExchangeRates;

/// <summary>
/// Latest FX snapshot for a base currency (6.2.3).
/// Serves last-known DB rates; on empty snapshot attempts one on-demand sync (6.2.5).
/// </summary>
public sealed record GetExchangeRatesQuery(string? Base = null) : IRequest<Result<ExchangeRatesResponse>>;

public sealed record ExchangeRatesResponse(
    string Base,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTimeOffset? LastUpdated,
    string? Source,
    bool IsStale,
    bool FromFallback,
    string? Message = null);

public sealed class GetExchangeRatesValidator : AbstractValidator<GetExchangeRatesQuery>
{
    public GetExchangeRatesValidator()
    {
        RuleFor(x => x.Base!)
            .Must(SupportedCurrencies.IsSupported)
            .WithMessage("Base currency is not supported.")
            .When(x => !string.IsNullOrWhiteSpace(x.Base));
    }
}

public sealed class GetExchangeRatesHandler
    : IRequestHandler<GetExchangeRatesQuery, Result<ExchangeRatesResponse>>
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(6);

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateSyncService _sync;
    private readonly IMemoryCache _cache;

    public GetExchangeRatesHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateSyncService sync,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _sync = sync;
        _cache = cache;
    }

    public async Task<Result<ExchangeRatesResponse>> Handle(
        GetExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ExchangeRatesResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        string bas;
        if (!string.IsNullOrWhiteSpace(request.Base))
        {
            if (!SupportedCurrencies.IsSupported(request.Base))
            {
                return Result.Failure<ExchangeRatesResponse>(DomainErrors.ExchangeRateErrors.InvalidBase);
            }

            bas = SupportedCurrencies.Normalize(request.Base);
        }
        else
        {
            var main = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => u.MainCurrency)
                .FirstOrDefaultAsync(cancellationToken);

            bas = SupportedCurrencies.Normalize(main);
        }

        var cacheKey = $"exchange-rates:{bas}";
        if (_cache.TryGetValue(cacheKey, out ExchangeRatesResponse? cached) && cached is not null)
        {
            return Result.Success(cached);
        }

        var response = await LoadFromDbAsync(bas, fromFallback: false, cancellationToken);

        // On-demand fetch when no snapshot yet (first run / empty DB)
        if (response is null || response.Rates.Count == 0)
        {
            var sync = await _sync.SyncBaseAsync(bas, cancellationToken);
            response = await LoadFromDbAsync(
                bas,
                fromFallback: sync.UsedExistingFallback,
                cancellationToken);

            if (response is null || response.Rates.Count == 0)
            {
                return Result.Failure<ExchangeRatesResponse>(
                    DomainErrors.ExchangeRateErrors.ProviderUnavailable);
            }

            // Mark fallback if we only survived via prior snapshot after a failed fetch
            if (sync.UsedExistingFallback)
            {
                response = response with
                {
                    FromFallback = true,
                    IsStale = true,
                    Message = "Serving last-known rates; live provider was unavailable."
                };
            }
        }

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        return Result.Success(response);
    }

    private async Task<ExchangeRatesResponse?> LoadFromDbAsync(
        string bas,
        bool fromFallback,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ExchangeRateSnapshots
            .AsNoTracking()
            .Where(e => e.BaseCurrency == bas)
            .Select(e => new
            {
                e.TargetCurrency,
                e.Rate,
                e.FetchedAt,
                e.Source
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        // Latest row per target (SQLite-safe: materialize then group)
        var latest = rows
            .GroupBy(r => CurrencyConversion.Normalize(r.TargetCurrency), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.FetchedAt).First())
            .OrderBy(x => x.TargetCurrency, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rates = latest.ToDictionary(
            r => CurrencyConversion.Normalize(r.TargetCurrency),
            r => r.Rate,
            StringComparer.OrdinalIgnoreCase);

        var lastUpdated = latest.Max(r => r.FetchedAt);
        var source = latest
            .OrderByDescending(r => r.FetchedAt)
            .Select(r => r.Source)
            .FirstOrDefault();

        var isStale = DateTimeOffset.UtcNow - lastUpdated > StaleAfter;

        return new ExchangeRatesResponse(
            Base: bas,
            Rates: rates,
            LastUpdated: lastUpdated,
            Source: source,
            IsStale: isStale || fromFallback,
            FromFallback: fromFallback,
            Message: isStale && !fromFallback
                ? "Rates may be outdated; waiting for next sync."
                : null);
    }
}
