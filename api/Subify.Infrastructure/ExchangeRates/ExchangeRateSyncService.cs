using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Services;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.ExchangeRates;

/// <summary>
/// Persist FX snapshots (6.2.2). On provider failure, leaves last-known rows (6.2.5).
/// </summary>
public sealed class ExchangeRateSyncService : IExchangeRateSyncService
{
    private readonly IExchangeRateClient _client;
    private readonly SubifyDbContext _db;
    private readonly ExchangeRateOptions _options;
    private readonly ILogger<ExchangeRateSyncService> _logger;

    public ExchangeRateSyncService(
        IExchangeRateClient client,
        SubifyDbContext db,
        IOptions<ExchangeRateOptions> options,
        ILogger<ExchangeRateSyncService> logger)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExchangeRateSyncResult> SyncBaseAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var bas = CurrencyConversion.Normalize(baseCurrency);
        var targets = ResolveTargets(bas);

        if (!_options.Enabled)
        {
            var has = await HasAnySnapshotAsync(bas, cancellationToken);
            return new ExchangeRateSyncResult(
                BaseCurrency: bas,
                Succeeded: has,
                UsedExistingFallback: has,
                RatesPersisted: 0,
                FetchedAt: null,
                Source: null,
                ErrorMessage: has ? "Sync disabled; using last-known snapshot." : "Sync disabled and no snapshot.");
        }

        var fetch = await _client.FetchAsync(bas, targets, cancellationToken);
        if (fetch.IsFailure)
        {
            var has = await HasAnySnapshotAsync(bas, cancellationToken);
            _logger.LogWarning(
                "FX sync failed for {Base}: {Error}. Fallback snapshot present: {HasFallback}",
                bas,
                fetch.Error.Code,
                has);

            return new ExchangeRateSyncResult(
                BaseCurrency: bas,
                Succeeded: has,
                UsedExistingFallback: has,
                RatesPersisted: 0,
                FetchedAt: null,
                Source: null,
                ErrorMessage: fetch.Error.Description);
        }

        var quote = fetch.Value;
        // Staleness is about *our* last successful pull, not the provider's
        // published "rates as of" time (free open.er-api often updates once/day ~00:00 UTC).
        // Using provider time made fresh syncs still look 6h+ stale after midday.
        var fetchedAt = DateTimeOffset.UtcNow;
        var source = string.IsNullOrWhiteSpace(quote.Source) ? "unknown" : quote.Source;

        foreach (var (target, rate) in quote.Rates)
        {
            _db.ExchangeRateSnapshots.Add(new ExchangeRateSnapshot(
                bas,
                CurrencyConversion.Normalize(target),
                rate,
                source,
                fetchedAt));
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "FX snapshot persisted for {Base}: {Count} rates from {Source} @ {FetchedAt:o}",
            bas,
            quote.Rates.Count,
            source,
            fetchedAt);

        return new ExchangeRateSyncResult(
            BaseCurrency: bas,
            Succeeded: true,
            UsedExistingFallback: false,
            RatesPersisted: quote.Rates.Count,
            FetchedAt: fetchedAt,
            Source: source,
            ErrorMessage: null);
    }

    public async Task<IReadOnlyList<ExchangeRateSyncResult>> SyncAllAsync(
        CancellationToken cancellationToken = default)
    {
        var bases = ResolveBases();
        var results = new List<ExchangeRateSyncResult>(bases.Count);

        foreach (var bas in bases)
        {
            try
            {
                results.Add(await SyncBaseAsync(bas, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 8.4.3 / 6.2.4 — one base must not kill the whole job
                _logger.LogError(ex, "Unhandled FX sync error for base {Base}", bas);
                results.Add(new ExchangeRateSyncResult(
                    BaseCurrency: bas,
                    Succeeded: false,
                    UsedExistingFallback: await HasAnySnapshotAsync(bas, cancellationToken),
                    RatesPersisted: 0,
                    FetchedAt: null,
                    Source: null,
                    ErrorMessage: ex.Message));
            }
        }

        return results;
    }

    private IReadOnlyList<string> ResolveBases()
    {
        if (_options.BaseCurrencies is { Length: > 0 })
        {
            return _options.BaseCurrencies
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(CurrencyConversion.Normalize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return SupportedCurrencies.All.ToList();
    }

    private static IReadOnlyCollection<string> ResolveTargets(string baseCurrency) =>
        SupportedCurrencies.All
            .Where(c => !string.Equals(c, baseCurrency, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private Task<bool> HasAnySnapshotAsync(string baseCurrency, CancellationToken cancellationToken) =>
        _db.ExchangeRateSnapshots
            .AsNoTracking()
            .AnyAsync(e => e.BaseCurrency == baseCurrency, cancellationToken);
}
