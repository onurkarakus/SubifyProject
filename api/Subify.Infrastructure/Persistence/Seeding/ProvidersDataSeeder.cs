using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the initial provider catalog (task 2.3.6).
/// Idempotent per slug — including soft-deleted rows (task 2.3.10).
/// Does not update existing rows (admin/custom prices preserved).
/// </summary>
public sealed class ProvidersDataSeeder : IDataSeeder
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<ProvidersDataSeeder> _logger;

    public ProvidersDataSeeder(
        SubifyDbContext db,
        ILogger<ProvidersDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int Order => 30;

    public string Name => "Providers";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = await _db.Providers
            .IgnoreQueryFilters()
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken);

        var existing = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var definition in SystemProviders.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existing.Contains(definition.Slug))
            {
                _logger.LogDebug("Provider {Slug} already exists; skipping.", definition.Slug);
                continue;
            }

            var provider = Provider.CreateCatalog(
                definition.Name,
                definition.Slug,
                definition.Currency,
                definition.Price,
                definition.BillingCycle,
                definition.Region,
                definition.SourceUrl,
                definition.LogoUrl);

            await _db.Providers.AddAsync(provider, cancellationToken);
            existing.Add(definition.Slug);
            added++;

            _logger.LogInformation("Seeded provider {Slug} ({Name}).", definition.Slug, definition.Name);
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Providers seeder inserted {Count} row(s).", added);
        }
        else
        {
            _logger.LogDebug("Providers seeder: all catalog providers already present.");
        }
    }
}
