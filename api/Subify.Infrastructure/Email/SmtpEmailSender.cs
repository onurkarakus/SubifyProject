using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.Email;

/// <summary>
/// SMTP sender reading live SystemSettings (15.1.2).
/// Uses <see cref="SmtpClient"/> for zero extra package weight on self-host.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(SubifyDbContext db, ILogger<SmtpEmailSender> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        return settings?.HasSmtpConfigured == true;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.ToEmail))
        {
            return Result.Failure(DomainErrors.Auth.InvalidEmailFormat);
        }

        var settings = await LoadSettingsAsync(cancellationToken);
        if (settings is null || !settings.HasSmtpConfigured)
        {
            return Result.Failure(DomainErrors.SystemSettingsErrors.SmtpNotConfigured);
        }

        try
        {
            using var client = CreateClient(settings);
            using var mail = new MailMessage
            {
                From = new MailAddress(
                    settings.SmtpFromEmail!,
                    string.IsNullOrWhiteSpace(settings.SmtpFromName)
                        ? settings.InstanceName ?? "Subify"
                        : settings.SmtpFromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(string.IsNullOrWhiteSpace(message.ToName)
                ? new MailAddress(message.ToEmail.Trim())
                : new MailAddress(message.ToEmail.Trim(), message.ToName));

            // SmtpClient has no native CT cancel for all platforms — honor token before send.
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mail, cancellationToken);

            _logger.LogInformation(
                "SMTP mail sent to {To} subject={Subject}",
                message.ToEmail,
                message.Subject);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To}", message.ToEmail);
            return Result.Failure(DomainErrors.SystemSettingsErrors.SmtpTestFailed);
        }
    }

    private async Task<SystemSettings?> LoadSettingsAsync(CancellationToken cancellationToken) =>
        // Singleton row; avoid SQLite DateTimeOffset ORDER BY
        await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    private static SmtpClient CreateClient(SystemSettings settings)
    {
        var port = settings.SmtpPort ?? 587;
        var client = new SmtpClient(settings.SmtpHost, port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = port is 465 or 587 or >= 2500,
            Timeout = 30_000
        };

        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
        {
            client.Credentials = new NetworkCredential(
                settings.SmtpUser,
                settings.SmtpPassword ?? string.Empty);
        }

        return client;
    }
}
