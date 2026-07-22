using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds TR/EN UI strings: Common, Category, Dashboard, Subscription, Error (task 2.3.7).
/// Idempotent on (PageName, Name, LanguageCode). Never seeds Paywall (OS).
/// </summary>
public sealed class ResourcesDataSeeder : IDataSeeder
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<ResourcesDataSeeder> _logger;

    public ResourcesDataSeeder(
        SubifyDbContext db,
        ILogger<ResourcesDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int Order => 40;

    public string Name => "Resources";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingKeys = await _db.Resources
            .Select(r => new { r.PageName, r.Name, r.LanguageCode })
            .ToListAsync(cancellationToken);

        var existing = existingKeys
            .Select(k => Key(k.PageName, k.Name, k.LanguageCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var definition in SystemResources.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = Key(definition.PageName, definition.Name, definition.LanguageCode);
            if (existing.Contains(key))
            {
                continue;
            }

            var resource = Resource.Create(
                definition.PageName,
                definition.Name,
                definition.LanguageCode,
                definition.Value);

            await _db.Resources.AddAsync(resource, cancellationToken);
            existing.Add(key);
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Resources seeder inserted {Count} row(s) (catalog size {Catalog}).",
                added,
                SystemResources.All.Count);
        }
        else
        {
            _logger.LogDebug("Resources seeder: all catalog rows already present.");
        }
    }

    private static string Key(string pageName, string name, string languageCode) =>
        $"{pageName}|{name}|{languageCode}";
}
