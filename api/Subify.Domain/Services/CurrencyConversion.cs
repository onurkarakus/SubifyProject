namespace Subify.Domain.Services;

/// <summary>
/// Simple multi-currency conversion using snapshot rates (4.3.4).
/// Rate dictionary key: (Base, Target) meaning 1 Base = Rate Target.
/// Inverse pairs are resolved automatically when only one direction is stored.
/// </summary>
public static class CurrencyConversion
{
    public static string Normalize(string currency) =>
        currency.Trim().ToUpperInvariant();

    /// <summary>
    /// Converts <paramref name="amount"/> from <paramref name="fromCurrency"/> to <paramref name="toCurrency"/>.
    /// Same currency → identity. Missing rate → original amount + warning (not converted).
    /// </summary>
    public static CurrencyConversionResult Convert(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        IReadOnlyDictionary<(string From, string To), decimal> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        var from = Normalize(fromCurrency);
        var to = Normalize(toCurrency);

        if (from == to)
        {
            return CurrencyConversionResult.Converted(amount, to, rateUsed: 1m);
        }

        if (rates.TryGetValue((from, to), out var direct) && direct > 0)
        {
            var converted = decimal.Round(amount * direct, 2, MidpointRounding.AwayFromZero);
            return CurrencyConversionResult.Converted(converted, to, rateUsed: direct);
        }

        if (rates.TryGetValue((to, from), out var inverse) && inverse > 0)
        {
            var converted = decimal.Round(amount / inverse, 2, MidpointRounding.AwayFromZero);
            return CurrencyConversionResult.Converted(converted, to, rateUsed: 1m / inverse);
        }

        return CurrencyConversionResult.Unconverted(
            amount,
            from,
            warning: $"No exchange rate for {from}→{to}; amount left in {from}.");
    }

    /// <summary>
    /// Latest-rate map from snapshot rows. Later rows with same pair win when ordered newest-first.
    /// </summary>
    public static IReadOnlyDictionary<(string From, string To), decimal> BuildRateMap(
        IEnumerable<(string Base, string Target, decimal Rate, DateTimeOffset FetchedAt)> snapshots)
    {
        var map = new Dictionary<(string, string), decimal>();
        foreach (var row in snapshots
                     .OrderByDescending(s => s.FetchedAt)
                     .ThenByDescending(s => s.Rate))
        {
            if (row.Rate <= 0)
            {
                continue;
            }

            var key = (Normalize(row.Base), Normalize(row.Target));
            if (key.Item1 == key.Item2)
            {
                continue;
            }

            map.TryAdd(key, row.Rate);
        }

        return map;
    }
}

public sealed record CurrencyConversionResult(
    decimal Amount,
    string Currency,
    bool WasConverted,
    decimal? RateUsed,
    string? Warning)
{
    public static CurrencyConversionResult Converted(decimal amount, string currency, decimal rateUsed) =>
        new(amount, currency, WasConverted: true, RateUsed: rateUsed, Warning: null);

    public static CurrencyConversionResult Unconverted(decimal amount, string originalCurrency, string warning) =>
        new(amount, originalCurrency, WasConverted: false, RateUsed: null, Warning: warning);
}
