using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the 10 system categories (task 2.3.5).
/// Idempotent per slug — including soft-deleted rows (unique index) (task 2.3.10).
/// </summary>
public sealed class CategoriesDataSeeder : IDataSeeder
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<CategoriesDataSeeder> _logger;

    public CategoriesDataSeeder(
        SubifyDbContext db,
        ILogger<CategoriesDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int Order => 20;

    public string Name => "Categories";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Ignore soft-delete filter: unique Slug index includes deleted rows
        var existingSlugs = await _db.Categories
            .IgnoreQueryFilters()
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);

        var existing = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var definition in SystemCategories.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existing.Contains(definition.Slug))
            {
                _logger.LogDebug("Category {Slug} already exists; skipping.", definition.Slug);
                continue;
            }

            var category = Category.CreateSystem(
                definition.Slug,
                definition.Icon,
                definition.Color,
                definition.SortOrder);

            await _db.Categories.AddAsync(category, cancellationToken);
            existing.Add(definition.Slug);
            added++;

            _logger.LogInformation("Seeded system category {Slug}.", definition.Slug);
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Categories seeder inserted {Count} row(s).", added);
        }
        else
        {
            _logger.LogDebug("Categories seeder: all system categories already present.");
        }
    }
}
