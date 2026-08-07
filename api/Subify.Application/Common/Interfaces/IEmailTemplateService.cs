using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>Load + render localized email templates.</summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders subject + HTML body. Prefers DB <c>EmailTemplates</c>, falls back to catalog seed definitions.
    /// </summary>
    Task<Result<RenderedEmailTemplate>> RenderAsync(
        string templateName,
        string? locale,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default);
}

public sealed record RenderedEmailTemplate(string Subject, string HtmlBody);
