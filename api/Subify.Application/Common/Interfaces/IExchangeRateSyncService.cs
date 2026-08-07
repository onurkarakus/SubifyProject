namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Fetches rates via <see cref="IExchangeRateClient"/> and persists <c>ExchangeRateSnapshot</c> rows (6.2.2).
/// On provider failure keeps last-known snapshots (6.2.5).
/// </summary>
public interface IExchangeRateSyncService
{
    /// <summary>Sync one base currency against supported targets.</summary>
    Task<ExchangeRateSyncResult> SyncBaseAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>Sync all configured / supported base currencies. Isolates per-base failures.</summary>
    Task<IReadOnlyList<ExchangeRateSyncResult>> SyncAllAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ExchangeRateSyncResult(
    string BaseCurrency,
    bool Succeeded,
    bool UsedExistingFallback,
    int RatesPersisted,
    DateTimeOffset? FetchedAt,
    string? Source,
    string? ErrorMessage);
