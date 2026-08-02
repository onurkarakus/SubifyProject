using Subify.Domain.Enums;

namespace Subify.Domain.Services;

/// <summary>
/// Pure subscription financial helpers (4.3.1 / 4.3.2 / 4.3.3 / 4.3.4).
/// Matches <see cref="Entities.Subscription"/> computed properties.
/// </summary>
public static class SubscriptionMath
{
    /// <summary>User share: Price / SharedWithCount (min 1 assumed by domain).</summary>
    public static decimal UserShare(decimal price, int sharedWithCount) =>
        sharedWithCount > 0
            ? decimal.Round(price / sharedWithCount, 2, MidpointRounding.AwayFromZero)
            : price;

    /// <summary>Normalize user share to a monthly amount.</summary>
    public static decimal MonthlyEquivalent(decimal userShare, BillingCycle billingCycle) =>
        billingCycle switch
        {
            BillingCycle.Yearly => decimal.Round(userShare / 12m, 2, MidpointRounding.AwayFromZero),
            _ => userShare
        };

    /// <summary>Normalize user share to a yearly amount.</summary>
    public static decimal YearlyEquivalent(decimal userShare, BillingCycle billingCycle) =>
        billingCycle switch
        {
            BillingCycle.Monthly => decimal.Round(userShare * 12m, 2, MidpointRounding.AwayFromZero),
            _ => userShare
        };

    public static decimal MonthlyEquivalentFromPrice(
        decimal price,
        int sharedWithCount,
        BillingCycle billingCycle) =>
        MonthlyEquivalent(UserShare(price, sharedWithCount), billingCycle);

    public static decimal YearlyEquivalentFromPrice(
        decimal price,
        int sharedWithCount,
        BillingCycle billingCycle) =>
        YearlyEquivalent(UserShare(price, sharedWithCount), billingCycle);

    /// <summary>
    /// Sum monthly/yearly equivalents for items already in <paramref name="mainCurrency"/> only
    /// (no FX). Prefer <see cref="SumConverted"/> when rates are available.
    /// </summary>
    public static SubscriptionTotalsSummary SumInCurrency(
        IEnumerable<SubscriptionAmountLine> lines,
        string mainCurrency) =>
        SumConverted(lines, mainCurrency, rates: null);

    /// <summary>
    /// Sum all lines into mainCurrency using snapshot rates (4.3.4).
    /// Missing rate: amount is <b>not</b> mixed into the main total; a warning is recorded
    /// (original currency left as-is conceptually; total stays pure mainCurrency).
    /// </summary>
    public static SubscriptionTotalsSummary SumConverted(
        IEnumerable<SubscriptionAmountLine> lines,
        string mainCurrency,
        IReadOnlyDictionary<(string From, string To), decimal>? rates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mainCurrency);
        var currency = CurrencyConversion.Normalize(mainCurrency);
        rates ??= new Dictionary<(string, string), decimal>();

        decimal monthly = 0m;
        decimal yearly = 0m;
        var warnings = new List<string>();
        var unconvertedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var monthlyLocal = MonthlyEquivalentFromPrice(line.Price, line.SharedWithCount, line.BillingCycle);
            var yearlyLocal = YearlyEquivalentFromPrice(line.Price, line.SharedWithCount, line.BillingCycle);

            var monthlyConv = CurrencyConversion.Convert(monthlyLocal, line.Currency, currency, rates);
            var yearlyConv = CurrencyConversion.Convert(yearlyLocal, line.Currency, currency, rates);

            if (monthlyConv.WasConverted && yearlyConv.WasConverted)
            {
                monthly += monthlyConv.Amount;
                yearly += yearlyConv.Amount;
            }
            else
            {
                var from = CurrencyConversion.Normalize(line.Currency);
                if (unconvertedCurrencies.Add(from))
                {
                    warnings.Add(
                        monthlyConv.Warning
                        ?? yearlyConv.Warning
                        ?? $"No exchange rate for {from}→{currency}; amounts excluded from {currency} total.");
                }
            }
        }

        return new SubscriptionTotalsSummary(
            MonthlyTotal: monthly,
            YearlyTotal: yearly,
            Currency: currency,
            Warnings: warnings,
            HasUnconvertedAmounts: warnings.Count > 0);
    }
}

/// <summary>Minimal financial projection for totals (no entity dependency required).</summary>
public readonly record struct SubscriptionAmountLine(
    decimal Price,
    int SharedWithCount,
    BillingCycle BillingCycle,
    string Currency);

/// <summary>Dashboard / list summary totals (4.1.5 / 4.3.3 / 4.3.4).</summary>
public sealed record SubscriptionTotalsSummary(
    decimal MonthlyTotal,
    decimal YearlyTotal,
    string Currency,
    IReadOnlyList<string> Warnings,
    bool HasUnconvertedAmounts)
{
    public SubscriptionTotalsSummary(decimal MonthlyTotal, decimal YearlyTotal, string Currency)
        : this(MonthlyTotal, YearlyTotal, Currency, Array.Empty<string>(), false)
    {
    }
}
