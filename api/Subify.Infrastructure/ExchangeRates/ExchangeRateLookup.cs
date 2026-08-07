using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Services;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.ExchangeRates;

/// <summary>Reads latest <c>ExchangeRateSnapshots</c> into a conversion map (4.3.4).</summary>
public sealed class ExchangeRateLookup : IExchangeRateLookup
{
    private readonly SubifyDbContext _db;

    public ExchangeRateLookup(SubifyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<(string From, string To), decimal>> GetLatestRateMapAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.ExchangeRateSnapshots
            .AsNoTracking()
            .Select(e => new
            {
                e.BaseCurrency,
                e.TargetCurrency,
                e.Rate,
                e.FetchedAt
            })
            .ToListAsync(cancellationToken);

        return CurrencyConversion.BuildRateMap(
            rows.Select(r => (r.BaseCurrency, r.TargetCurrency, r.Rate, r.FetchedAt)));
    }
}
