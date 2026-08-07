using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;

namespace Subify.Application.Common.Localization;

/// <summary>
/// Resolves UI/API locale: explicit arg → Accept-Language → user profile → default.
/// </summary>
public static class LocaleResolver
{
    public static string Resolve(
        string? explicitLocale,
        string? acceptLanguageHeader,
        ICurrentUserService? currentUser)
    {
        if (!string.IsNullOrWhiteSpace(explicitLocale) && SupportedLocales.IsSupported(explicitLocale))
        {
            return SupportedLocales.Normalize(explicitLocale);
        }

        var fromHeader = ParseAcceptLanguage(acceptLanguageHeader);
        if (fromHeader is not null)
        {
            return fromHeader;
        }

        if (currentUser is { IsAuthenticated: true }
            && !string.IsNullOrWhiteSpace(currentUser.Locale)
            && SupportedLocales.IsSupported(currentUser.Locale))
        {
            return SupportedLocales.Normalize(currentUser.Locale);
        }

        return SupportedLocales.Default;
    }

    /// <summary>
    /// Picks first supported tag from Accept-Language (e.g. "tr-TR,tr;q=0.9,en;q=0.8" → tr).
    /// </summary>
    public static string? ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tag = part.Split(';', 2)[0].Trim();
            if (tag.Length == 0)
            {
                continue;
            }

            // tr-TR → tr
            var primary = tag.Split('-', 2)[0];
            if (SupportedLocales.IsSupported(primary))
            {
                return SupportedLocales.Normalize(primary);
            }

            if (SupportedLocales.IsSupported(tag))
            {
                return SupportedLocales.Normalize(tag);
            }
        }

        return null;
    }
}
