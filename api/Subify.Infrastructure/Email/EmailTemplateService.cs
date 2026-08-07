using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Subify.Application.Common.Email;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.Email;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly SubifyDbContext _db;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(SubifyDbContext db, ILogger<EmailTemplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<RenderedEmailTemplate>> RenderAsync(
        string templateName,
        string? locale,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return Result.Failure<RenderedEmailTemplate>(
                DomainErrors.ValidationErrors.RequiredFieldMissing);
        }

        var lang = SupportedLocales.Normalize(locale);
        var name = templateName.Trim();

        var row = await _db.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Name == name && t.LanguageCode == lang,
                cancellationToken);

        if (row is null && lang != SupportedLocales.Default)
        {
            row = await _db.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Name == name && t.LanguageCode == SupportedLocales.Default,
                    cancellationToken);
        }

        string subject;
        string body;

        if (row is not null)
        {
            subject = row.Subject;
            body = row.Body;
        }
        else
        {
            var catalog = SystemEmailTemplates.All
                .FirstOrDefault(t =>
                    t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase))
                ?? SystemEmailTemplates.All.FirstOrDefault(t =>
                    t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && t.LanguageCode.Equals(SupportedLocales.Default, StringComparison.OrdinalIgnoreCase))
                ?? SystemEmailTemplates.All.FirstOrDefault(t =>
                    t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (catalog is null)
            {
                _logger.LogWarning("Email template {Name}/{Lang} not found", name, lang);
                return Result.Failure<RenderedEmailTemplate>(
                    DomainErrors.ResourceErrors.ResourceNotFound);
            }

            subject = catalog.Subject;
            body = catalog.Body;
        }

        var renderedSubject = EmailTemplateRenderer.Render(subject, tokens);
        var renderedBody = EmailTemplateRenderer.Render(body, tokens);

        return Result.Success(new RenderedEmailTemplate(renderedSubject, renderedBody));
    }
}
