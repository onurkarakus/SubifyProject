using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Outbound email attempt log. Used for audit and renewal duplicate protection (15.3.2).
/// </summary>
public class EmailSendLog : BaseEntity
{
    public const int TemplateNameMaxLength = 100;
    public const int ToEmailMaxLength = 320;
    public const int DedupeKeyMaxLength = 200;
    public const int ErrorMaxLength = 1000;

    public string TemplateName { get; private set; } = string.Empty;
    public string ToEmail { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>Unique successful-send key; null when not deduped.</summary>
    public string? DedupeKey { get; private set; }

    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    protected EmailSendLog()
    {
    }

    public static EmailSendLog Create(
        string templateName,
        string toEmail,
        bool success,
        Guid? userId = null,
        Guid? relatedEntityId = null,
        string? dedupeKey = null,
        string? error = null,
        DateTimeOffset? sentAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        return new EmailSendLog
        {
            Id = GuidGenerator.NewId(),
            TemplateName = templateName.Trim(),
            ToEmail = toEmail.Trim().ToLowerInvariant(),
            UserId = userId,
            RelatedEntityId = relatedEntityId,
            DedupeKey = string.IsNullOrWhiteSpace(dedupeKey) ? null : dedupeKey.Trim(),
            Success = success,
            Error = string.IsNullOrWhiteSpace(error)
                ? null
                : (error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength]),
            SentAt = sentAt ?? DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
