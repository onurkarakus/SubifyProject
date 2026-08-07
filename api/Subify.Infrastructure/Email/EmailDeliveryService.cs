using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.Email;

public sealed class EmailDeliveryService : IEmailDeliveryService
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _templates;
    private readonly SubifyDbContext _db;
    private readonly ILogger<EmailDeliveryService> _logger;

    public EmailDeliveryService(
        IEmailSender emailSender,
        IEmailTemplateService templates,
        SubifyDbContext db,
        ILogger<EmailDeliveryService> logger)
    {
        _emailSender = emailSender;
        _templates = templates;
        _db = db;
        _logger = logger;
    }

    public async Task<Result> SendTemplatedAsync(
        string templateName,
        string? locale,
        string toEmail,
        IReadOnlyDictionary<string, string> tokens,
        Guid? userId = null,
        Guid? relatedEntityId = null,
        string? dedupeKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(dedupeKey))
        {
            var exists = await _db.EmailSendLogs
                .AsNoTracking()
                .AnyAsync(
                    l => l.DedupeKey == dedupeKey && l.Success,
                    cancellationToken);

            if (exists)
            {
                _logger.LogDebug("Email dedupe skip key={DedupeKey}", dedupeKey);
                return Result.Success();
            }
        }

        var rendered = await _templates.RenderAsync(templateName, locale, tokens, cancellationToken);
        if (rendered.IsFailure)
        {
            return Result.Failure(rendered.Error);
        }

        var send = await _emailSender.SendAsync(
            new EmailMessage(toEmail, rendered.Value.Subject, rendered.Value.HtmlBody),
            cancellationToken);

        var log = EmailSendLog.Create(
            templateName: templateName,
            toEmail: toEmail,
            success: send.IsSuccess,
            userId: userId,
            relatedEntityId: relatedEntityId,
            dedupeKey: send.IsSuccess ? dedupeKey : null,
            error: send.IsFailure ? send.Error.Code + ": " + send.Error.Description : null);

        _db.EmailSendLogs.Add(log);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (!string.IsNullOrWhiteSpace(dedupeKey))
        {
            // Unique dedupe race — treat as already sent
            _logger.LogDebug(ex, "Email dedupe unique conflict key={DedupeKey}", dedupeKey);
            return Result.Success();
        }

        return send;
    }
}
