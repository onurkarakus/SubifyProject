using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Localized HTML email template for outbound SMTP (reset, invite, renewal, report summary, …).
/// Unique key: (Name, LanguageCode).
/// </summary>
public class EmailTemplates : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    protected EmailTemplates()
    {
    }

    /// <summary>Creates a template row for seed (task 2.3.8).</summary>
    public static EmailTemplates Create(string name, string languageCode, string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new EmailTemplates
        {
            Id = GuidGenerator.NewId(),
            Name = name.Trim(),
            LanguageCode = languageCode.Trim().ToLowerInvariant(),
            Subject = subject.Trim(),
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string languageCode, string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        Name = name.Trim();
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        Body = body;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Admin edit of content only (keeps unique Name+LanguageCode key).</summary>
    public void UpdateContent(string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        Subject = subject.Trim();
        Body = body;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
