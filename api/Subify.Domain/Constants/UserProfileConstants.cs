namespace Subify.Domain.Constants;

/// <summary>
/// User profile constraints aligned with Subify OS PRD (no freemium plan fields).
/// </summary>
public static class UserProfileConstants
{
    public const int FullNameMaxLength = 200;
    public const int LocaleMaxLength = 10;
    public const int MainCurrencyMaxLength = 10;
    public const int ThemeColorMaxLength = 50;

    /// <summary>MonthlyBudget precision: decimal(10,2).</summary>
    public const int MonthlyBudgetPrecision = 10;
    public const int MonthlyBudgetScale = 2;
}

/// <summary>Supported UI/API locales (ISO-like short codes).</summary>
public static class SupportedLocales
{
    public const string Default = Tr;
    public const string Tr = "tr";
    public const string En = "en";

    public static readonly IReadOnlyList<string> All = [Tr, En];

    public static bool IsSupported(string? locale) =>
        !string.IsNullOrWhiteSpace(locale)
        && All.Contains(locale.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? locale) =>
        IsSupported(locale) ? locale!.Trim().ToLowerInvariant() : Default;
}

/// <summary>Common ISO 4217 currencies for MainCurrency.</summary>
public static class SupportedCurrencies
{
    public const string Default = Try;
    public const string Try = "TRY";
    public const string Usd = "USD";
    public const string Eur = "EUR";
    public const string Gbp = "GBP";

    public static readonly IReadOnlyList<string> All = [Try, Usd, Eur, Gbp];

    public static bool IsSupported(string? currency) =>
        !string.IsNullOrWhiteSpace(currency)
        && All.Contains(currency.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? currency) =>
        IsSupported(currency) ? currency!.Trim().ToUpperInvariant() : Default;
}

/// <summary>User-selectable accent theme names (PRD presets).</summary>
public static class ThemeColors
{
    public const string Default = RoyalPurple;

    public const string RoyalPurple = "Royal Purple";
    public const string OceanBlue = "Ocean Blue";
    public const string ForestGreen = "Forest Green";
    public const string SunsetOrange = "Sunset Orange";
    public const string CherryRed = "Cherry Red";
    public const string GoldenYellow = "Golden Yellow";

    public static readonly IReadOnlyList<string> All =
    [
        RoyalPurple,
        OceanBlue,
        ForestGreen,
        SunsetOrange,
        CherryRed,
        GoldenYellow
    ];

    public static bool IsSupported(string? themeColor) =>
        !string.IsNullOrWhiteSpace(themeColor)
        && All.Contains(themeColor.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? themeColor)
    {
        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return Default;
        }

        var match = All.FirstOrDefault(c =>
            string.Equals(c, themeColor.Trim(), StringComparison.OrdinalIgnoreCase));

        return match ?? Default;
    }
}
