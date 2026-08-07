using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>
/// External FX HTTP provider (6.2.1). Does not touch the database.
/// Rate map: 1 <see cref="ExchangeRateFetchResult.BaseCurrency"/> = Rate <c>target</c>.
/// </summary>
public interface IExchangeRateClient
{
    /// <summary>
    /// Fetches latest rates for <paramref name="baseCurrency"/>.
    /// When <paramref name="targetCurrencies"/> is set, only those pairs are returned (plus base skipped).
    /// </summary>
    Task<Result<ExchangeRateFetchResult>> FetchAsync(
        string baseCurrency,
        IReadOnlyCollection<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Live quote from an FX provider.</summary>
public sealed record ExchangeRateFetchResult(
    string BaseCurrency,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTimeOffset FetchedAt,
    string Source);
