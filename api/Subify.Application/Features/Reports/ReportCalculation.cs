using Subify.Domain.Enums;
using Subify.Domain.Services;

namespace Subify.Application.Features.Reports;

/// <summary>Shared report math helpers (6.1).</summary>
internal static class ReportCalculation
{
    public static decimal Percentage(decimal part, decimal whole) =>
        whole <= 0m
            ? 0m
            : decimal.Round(part / whole * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Monthly user-share converted to <paramref name="targetCurrency"/>.
    /// Missing rate → 0 (excluded from main-currency totals, same as list summary).
    /// </summary>
    public static decimal ConvertedMonthly(
        decimal price,
        int sharedWithCount,
        BillingCycle billingCycle,
        string sourceCurrency,
        string targetCurrency,
        IReadOnlyDictionary<(string From, string To), decimal> rates)
    {
        var local = SubscriptionMath.MonthlyEquivalentFromPrice(price, sharedWithCount, billingCycle);
        var converted = CurrencyConversion.Convert(local, sourceCurrency, targetCurrency, rates);
        return converted.WasConverted ? converted.Amount : 0m;
    }

    public static decimal LocalMonthly(
        decimal price,
        int sharedWithCount,
        BillingCycle billingCycle) =>
        SubscriptionMath.MonthlyEquivalentFromPrice(price, sharedWithCount, billingCycle);

    /// <summary>
    /// Builds last <paramref name="months"/> calendar month keys ending at <paramref name="asOf"/> (UTC), oldest first.
    /// </summary>
    public static IReadOnlyList<(string Key, DateTimeOffset Start, DateTimeOffset EndExclusive)> BuildMonthWindows(
        int months,
        DateTimeOffset asOf)
    {
        var utc = asOf.ToUniversalTime();
        var currentMonthStart = new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var list = new List<(string, DateTimeOffset, DateTimeOffset)>(months);

        for (var i = months - 1; i >= 0; i--)
        {
            var start = currentMonthStart.AddMonths(-i);
            var end = start.AddMonths(1);
            var key = $"{start.Year:D4}-{start.Month:D2}";
            list.Add((key, start, end));
        }

        return list;
    }

    /// <summary>
    /// Subscription counted in a month if created before month end and not archived before month start.
    /// </summary>
    public static bool WasActiveDuring(
        DateTimeOffset createdAt,
        DateTimeOffset? archivedAt,
        DateTimeOffset monthStart,
        DateTimeOffset monthEndExclusive) =>
        createdAt < monthEndExclusive
        && (archivedAt is null || archivedAt >= monthStart);
}
