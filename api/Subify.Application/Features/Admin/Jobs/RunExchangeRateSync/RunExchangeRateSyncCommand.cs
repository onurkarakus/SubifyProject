using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Jobs.RunExchangeRateSync;

/// <summary>
/// SuperAdmin ops: force one live FX fetch for a base (or instance default),
/// invalidate GET cache, return latest snapshot.
/// </summary>
public sealed record RunExchangeRateSyncCommand(string? Base = null)
    : IRequest<Result<RunExchangeRateSyncResponse>>;

public sealed record RunExchangeRateSyncResponse(
    string Base,
    bool Succeeded,
    bool UsedExistingFallback,
    int RatesPersisted,
    DateTimeOffset? FetchedAt,
    string? Source,
    bool IsStale,
    IReadOnlyDictionary<string, decimal> Rates,
    string? Message = null,
    string? ErrorMessage = null);

public sealed class RunExchangeRateSyncHandler
    : IRequestHandler<RunExchangeRateSyncCommand, Result<RunExchangeRateSyncResponse>>
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(6);

    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateSyncService _sync;
    private readonly ISubifyDbContext _db;
    private readonly IMemoryCache _cache;

    public RunExchangeRateSyncHandler(
        ICurrentUserService currentUser,
        IExchangeRateSyncService sync,
        ISubifyDbContext db,
        IMemoryCache cache)
    {
        _currentUser = currentUser;
        _sync = sync;
        _db = db;
        _cache = cache;
    }

    public async Task<Result<RunExchangeRateSyncResponse>> Handle(
        RunExchangeRateSyncCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<RunExchangeRateSyncResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<RunExchangeRateSyncResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        string bas;
        if (!string.IsNullOrWhiteSpace(request.Base))
        {
            if (!SupportedCurrencies.IsSupported(request.Base))
            {
                return Result.Failure<RunExchangeRateSyncResponse>(DomainErrors.ExchangeRateErrors.InvalidBase);
            }

            bas = SupportedCurrencies.Normalize(request.Base);
        }
        else
        {
            var instanceDefault = await _db.SystemSettings
                .AsNoTracking()
                .Select(s => s.DefaultCurrency)
                .FirstOrDefaultAsync(cancellationToken);

            bas = SupportedCurrencies.Normalize(instanceDefault);
        }

        var sync = await _sync.SyncBaseAsync(bas, cancellationToken);

        // Bust GET /exchange-rates memory cache so UI sees fresh data immediately.
        _cache.Remove($"exchange-rates:{bas}");

        var snapshot = await LoadFromDbAsync(bas, cancellationToken);
        var rates = snapshot?.Rates ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var lastUpdated = snapshot?.LastUpdated ?? sync.FetchedAt;
        var source = snapshot?.Source ?? sync.Source;
        var isStale = lastUpdated is null
            || DateTimeOffset.UtcNow - lastUpdated > StaleAfter
            || sync.UsedExistingFallback;

        string? message;
        if (sync.Succeeded && !sync.UsedExistingFallback)
        {
            message = $"Live sync OK: {sync.RatesPersisted} rates.";
        }
        else if (sync.UsedExistingFallback && rates.Count > 0)
        {
            message = sync.ErrorMessage
                      ?? "Live provider failed; last-known snapshot kept.";
        }
        else
        {
            message = sync.ErrorMessage ?? "Exchange rate sync failed.";
        }

        if (rates.Count == 0)
        {
            return Result.Failure<RunExchangeRateSyncResponse>(
                DomainErrors.ExchangeRateErrors.ProviderUnavailable);
        }

        return Result.Success(new RunExchangeRateSyncResponse(
            Base: bas,
            Succeeded: sync.Succeeded && !sync.UsedExistingFallback,
            UsedExistingFallback: sync.UsedExistingFallback,
            RatesPersisted: sync.RatesPersisted,
            FetchedAt: lastUpdated,
            Source: source,
            IsStale: isStale,
            Rates: rates,
            Message: message,
            ErrorMessage: sync.ErrorMessage));
    }

    private async Task<(
        IReadOnlyDictionary<string, decimal> Rates,
        DateTimeOffset LastUpdated,
        string? Source)?> LoadFromDbAsync(string bas, CancellationToken cancellationToken)
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

        var latest = rows
            .GroupBy(r => CurrencyConversion.Normalize(r.TargetCurrency), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.FetchedAt).First())
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

        return (rates, lastUpdated, source);
    }
}
