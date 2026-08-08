using Microsoft.Extensions.Caching.Memory;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Resources;

/// <summary>IMemoryCache keys + invalidation for resource packs (6.3.2).</summary>
public static class ResourceCache
{
    public static string FullPackKey(string languageCode) =>
        $"resources:full:{SupportedLocales.Normalize(languageCode)}";

    public static void SetFullPack(
        IMemoryCache cache,
        string languageCode,
        ListResourcesResponse response,
        TimeSpan? ttl = null)
    {
        cache.Set(
            FullPackKey(languageCode),
            response,
            ttl ?? TimeSpan.FromSeconds(ResourceConstants.FullPackCacheSeconds));
    }

    public static bool TryGetFullPack(
        IMemoryCache cache,
        string languageCode,
        out ListResourcesResponse? response) =>
        cache.TryGetValue(FullPackKey(languageCode), out response);

    /// <summary>Drop cache for one language, or all supported languages when null.</summary>
    public static void Invalidate(IMemoryCache cache, string? languageCode = null)
    {
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            cache.Remove(FullPackKey(languageCode));
            return;
        }

        foreach (var lang in SupportedLocales.All)
        {
            cache.Remove(FullPackKey(lang));
        }
    }
}
