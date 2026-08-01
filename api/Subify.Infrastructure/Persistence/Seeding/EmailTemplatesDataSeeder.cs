using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds ResetPassword, RenewalReminder, Invite, ReportSummary templates (task 2.3.8).
/// No VerifyEmail. Templates only; SMTP sender uses these at runtime.
/// Idempotent on (Name, LanguageCode).
/// </summary>
public sealed class EmailTemplatesDataSeeder : IDataSeeder
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<EmailTemplatesDataSeeder> _logger;

    public EmailTemplatesDataSeeder(
        SubifyDbContext db,
        ILogger<EmailTemplatesDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public int Order => 60;

    public string Name => "EmailTemplates";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingKeys = await _db.EmailTemplates
            .Select(t => new { t.Name, t.LanguageCode })
            .ToListAsync(cancellationToken);

        var existing = existingKeys
            .Select(k => Key(k.Name, k.LanguageCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var definition in SystemEmailTemplates.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = Key(definition.Name, definition.LanguageCode);
            if (existing.Contains(key))
            {
                continue;
            }

            var template = EmailTemplates.Create(
                definition.Name,
                definition.LanguageCode,
                definition.Subject,
                definition.Body);

            await _db.EmailTemplates.AddAsync(template, cancellationToken);
            existing.Add(key);
            added++;

            _logger.LogInformation(
                "Seeded email template {Name}/{Lang}.",
                definition.Name,
                definition.LanguageCode);
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("EmailTemplates seeder inserted {Count} row(s).", added);
        }
        else
        {
            _logger.LogDebug("EmailTemplates seeder: all catalog templates already present.");
        }
    }

    private static string Key(string name, string languageCode) =>
        $"{name}|{languageCode}";
}
