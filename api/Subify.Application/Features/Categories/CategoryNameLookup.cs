using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;

namespace Subify.Application.Features.Categories;

/// <summary>
/// Resolves localized system category names from Resources (5.1.1 / 5.1.6).
/// Page = Category, Name = slug; fallback = slug.
/// </summary>
public interface ICategoryNameLookup
{
    /// <summary>
    /// Returns slug → display name for the requested locale (falls back to default locale, then slug).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        string locale,
        CancellationToken cancellationToken = default);

    string ResolveName(string slug, IReadOnlyDictionary<string, string> names);
}

public sealed class CategoryNameLookup : ICategoryNameLookup
{
    private readonly ISubifyDbContext _db;

    public CategoryNameLookup(ISubifyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        string locale,
        CancellationToken cancellationToken = default)
    {
        var lang = SupportedLocales.Normalize(locale);
        var fallback = SupportedLocales.Default;

        var rows = await _db.Resources
            .AsNoTracking()
            .Where(r =>
                r.PageName == SystemResources.Pages.Category
                && (r.LanguageCode == lang || r.LanguageCode == fallback))
            .Select(r => new { r.Name, r.LanguageCode, r.Value })
            .ToListAsync(cancellationToken);

        // Prefer requested language; fill gaps from default locale.
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Where(r =>
                     string.Equals(r.LanguageCode, lang, StringComparison.OrdinalIgnoreCase)))
        {
            map[row.Name] = row.Value;
        }

        if (lang != fallback)
        {
            foreach (var row in rows.Where(r =>
                         string.Equals(r.LanguageCode, fallback, StringComparison.OrdinalIgnoreCase)))
            {
                map.TryAdd(row.Name, row.Value);
            }
        }

        return map;
    }

    public string ResolveName(string slug, IReadOnlyDictionary<string, string> names)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return slug;
        }

        return names.TryGetValue(slug.Trim(), out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : slug.Trim();
    }
}
