using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Application.Common.Options;
using Subify.Domain.Constants;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.Email;

/// <summary>Core logic for <see cref="Background.RenewalReminderBackgroundService"/> (testable).</summary>
public sealed class RenewalReminderService : IRenewalReminderService
{
    private readonly SubifyDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IEmailDeliveryService _delivery;
    private readonly AppOptions _app;
    private readonly ILogger<RenewalReminderService> _logger;

    public RenewalReminderService(
        SubifyDbContext db,
        IEmailSender emailSender,
        IEmailDeliveryService delivery,
        IOptions<AppOptions> app,
        ILogger<RenewalReminderService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _delivery = delivery;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        if (!await _emailSender.IsConfiguredAsync(cancellationToken))
        {
            _logger.LogDebug("Renewal reminders skipped: SMTP not configured");
            return 0;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var prefs = await _db.NotificationSettings
            .AsNoTracking()
            .Where(n => n.EmailEnabled)
            .Select(n => new { n.UserId, n.DaysBeforeRenewal })
            .ToListAsync(cancellationToken);

        if (prefs.Count == 0)
        {
            return 0;
        }

        var processed = 0;

        foreach (var pref in prefs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var windowEnd = today.AddDays(Math.Clamp(pref.DaysBeforeRenewal, 0, 30));

            var subs = await _db.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.UserId == pref.UserId
                    && !s.Archived
                    && s.DeletedAt == null
                    && s.NextRenewalDate >= today
                    && s.NextRenewalDate <= windowEnd)
                .ToListAsync(cancellationToken);

            if (subs.Count == 0)
            {
                continue;
            }

            var user = await _db.Users.AsNoTracking()
                .Where(u => u.Id == pref.UserId && !u.IsDisabled)
                .Select(u => new { u.Id, u.Email, u.FullName, u.Locale })
                .FirstOrDefaultAsync(cancellationToken);

            if (user?.Email is null)
            {
                continue;
            }

            var locale = SupportedLocales.Normalize(user.Locale);

            foreach (var sub in subs)
            {
                var dedupe = $"renewal:{sub.Id:N}:{sub.NextRenewalDate:yyyy-MM-dd}";
                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FullName"] = user.FullName ?? user.Email,
                    ["SubscriptionName"] = sub.Name,
                    ["Amount"] = sub.Price.ToString("0.##"),
                    ["Currency"] = sub.Currency,
                    ["RenewalDate"] = sub.NextRenewalDate.ToString("yyyy-MM-dd"),
                    ["AppUrl"] = _app.BaseUrl
                };

                var result = await _delivery.SendTemplatedAsync(
                    templateName: SystemEmailTemplates.Names.RenewalReminder,
                    locale: locale,
                    toEmail: user.Email,
                    tokens: tokens,
                    userId: user.Id,
                    relatedEntityId: sub.Id,
                    dedupeKey: dedupe,
                    cancellationToken: cancellationToken);

                if (result.IsSuccess)
                {
                    processed++;
                }
            }
        }

        return processed;
    }
}
