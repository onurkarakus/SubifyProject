using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures a single SystemSettings row exists (task 2.3.9).
/// Idempotent: only inserts when the table is empty (singleton).
/// Defaults: IsSetupComplete=false, no secrets, SmtpEnabled=false.
/// </summary>
public sealed class SystemSettingsDataSeeder : IDataSeeder
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<SystemSettingsDataSeeder> _logger;

    public SystemSettingsDataSeeder(
        SubifyDbContext db,
        ILogger<SystemSettingsDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int Order => 50;

    public string Name => "SystemSettings";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var count = await _db.SystemSettings.CountAsync(cancellationToken);

        if (count > 0)
        {
            if (count > 1)
            {
                _logger.LogWarning(
                    "SystemSettings has {Count} rows; expected a singleton. Skipping seed.",
                    count);
            }
            else
            {
                _logger.LogDebug("SystemSettings singleton already present; skipping.");
            }

            return;
        }

        var settings = SystemSettings.CreateDefault();
        await _db.SystemSettings.AddAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded SystemSettings singleton (Id={Id}, IsSetupComplete={SetupComplete}, InstanceName={InstanceName}).",
            settings.Id,
            settings.IsSetupComplete,
            settings.InstanceName);
    }
}
