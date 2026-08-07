namespace Subify.Domain.Constants;

/// <summary>i18n resource field bounds (matches EF ResourceConfiguration / 6.3).</summary>
public static class ResourceConstants
{
    public const int PageNameMaxLength = 100;
    public const int NameMaxLength = 100;
    public const int LanguageCodeMaxLength = 5;
    public const int ValueMaxLength = 4000;

    /// <summary>Full language pack cache TTL (ADR: ~1 hour).</summary>
    public const int FullPackCacheSeconds = 3600;
}
