using Microsoft.AspNetCore.Identity;
using Subify.Domain.Constants;

namespace Subify.Domain.Entities;

/// <summary>
/// Identity user + OS profile preferences (single model — no separate profiles table, no plan/premium).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Display name (PRD PRO-01).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>UI/API locale: <c>tr</c> or <c>en</c>.</summary>
    public string Locale { get; set; } = SupportedLocales.Default;

    /// <summary>Preferred display currency (ISO 4217, e.g. TRY).</summary>
    public string MainCurrency { get; set; } = SupportedCurrencies.Default;

    /// <summary>Optional monthly budget; null or ≤0 means budget tracking disabled.</summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>Accent theme name from <see cref="ThemeColors"/> presets.</summary>
    public string ApplicationThemeColor { get; set; } = ThemeColors.Default;

    /// <summary>Dark theme preference.</summary>
    public bool DarkTheme { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Soft-disable flag (7.1.5). When true, login is rejected even if password is valid.
    /// Distinct from temporary Identity lockout (failed attempts / admin lock).
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>When the account was soft-disabled; null when active.</summary>
    public DateTimeOffset? DisabledAt { get; set; }

    /// <summary>
    /// Applies registration defaults for a new user (profile fields + Identity username/email).
    /// Email confirmation is always true (OS: no email confirm flow).
    /// </summary>
    public void ApplyRegistrationProfile(string fullName, string email)
    {
        var normalizedEmail = email.Trim();
        FullName = fullName.Trim();
        UserName = normalizedEmail;
        Email = normalizedEmail;
        EmailConfirmed = true;

        Locale = SupportedLocales.Default;
        MainCurrency = SupportedCurrencies.Default;
        MonthlyBudget = null;
        ApplicationThemeColor = ThemeColors.Default;
        DarkTheme = false;
        IsDisabled = false;
        DisabledAt = null;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = null;
    }

    /// <summary>
    /// Applies instance-wide defaults (locale/currency/theme) for new users (3S.3.2).
    /// Call after <see cref="ApplyRegistrationProfile"/> when SystemSettings is available.
    /// </summary>
    public void ApplyInstanceDefaults(
        string? locale = null,
        string? mainCurrency = null,
        string? applicationThemeColor = null,
        bool? darkTheme = null)
    {
        if (locale is not null)
        {
            Locale = SupportedLocales.Normalize(locale);
        }

        if (mainCurrency is not null)
        {
            MainCurrency = SupportedCurrencies.Normalize(mainCurrency);
        }

        if (applicationThemeColor is not null)
        {
            ApplicationThemeColor = ThemeColors.Normalize(applicationThemeColor);
        }

        if (darkTheme is not null)
        {
            DarkTheme = darkTheme.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Soft-disable account (7.1.5). Idempotent.</summary>
    public void Disable()
    {
        if (IsDisabled)
        {
            return;
        }

        IsDisabled = true;
        DisabledAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Re-enable a soft-disabled account. Idempotent.</summary>
    public void Enable()
    {
        if (!IsDisabled)
        {
            return;
        }

        IsDisabled = false;
        DisabledAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates profile preferences. Pass null for any field that should remain unchanged.
    /// </summary>
    public void UpdateProfile(
        string? fullName = null,
        string? locale = null,
        string? mainCurrency = null,
        decimal? monthlyBudget = null,
        bool clearMonthlyBudget = false,
        string? applicationThemeColor = null,
        bool? darkTheme = null)
    {
        if (fullName is not null)
        {
            FullName = fullName.Trim();
        }

        if (locale is not null)
        {
            Locale = SupportedLocales.Normalize(locale);
        }

        if (mainCurrency is not null)
        {
            MainCurrency = SupportedCurrencies.Normalize(mainCurrency);
        }

        if (clearMonthlyBudget)
        {
            MonthlyBudget = null;
        }
        else if (monthlyBudget is not null)
        {
            // 0 or negative → treat as disabled (null)
            MonthlyBudget = monthlyBudget > 0 ? monthlyBudget : null;
        }

        if (applicationThemeColor is not null)
        {
            ApplicationThemeColor = ThemeColors.Normalize(applicationThemeColor);
        }

        if (darkTheme is not null)
        {
            DarkTheme = darkTheme.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>True when a positive monthly budget is configured.</summary>
    public bool HasMonthlyBudget => MonthlyBudget is > 0;
}
