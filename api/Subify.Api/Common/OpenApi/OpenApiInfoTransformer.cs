using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Subify.Api.Common.OpenApi;

/// <summary>14.2.1 — Subify OS document title, version, description.</summary>
public sealed class OpenApiInfoTransformer : IOpenApiDocumentTransformer
{
    public const string Title = "Subify OS API";
    public const string Version = "1.0.0";

    public const string Description =
        "Self-hosted multi-user subscription tracker (Subify OS). " +
        "No freemium limits, no SaaS billing. JWT Bearer auth. " +
        "First-run setup: POST /api/setup/admin then complete setup. " +
        "Secrets (AI key, SMTP password) are never returned in plain text. " +
        "Outbound email (reset, invite, renewal reminders, report summary) when SMTP is enabled and configured.";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = Title;
        document.Info.Version = Version;
        document.Info.Description = Description;
        document.Info.Contact = new OpenApiContact
        {
            Name = "Subify OS"
        };

        return Task.CompletedTask;
    }
}
