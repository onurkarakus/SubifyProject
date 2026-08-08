using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Admin.EmailTemplates;
using Subify.Application.Features.Admin.EmailTemplates.GetEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.ListEmailTemplates;
using Subify.Application.Features.Admin.EmailTemplates.PreviewEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.TestSendEmailTemplate;
using Subify.Application.Features.Admin.EmailTemplates.UpdateEmailTemplate;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Admin;

/// <summary>Email template admin (7.4.1 list/get/update · 7.4.2 preview/test-send).</summary>
public sealed class AdminEmailTemplateEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/email-templates")
            .WithTags("Admin · Email templates")
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapGet("/", async (
                [FromQuery] string? name,
                [FromQuery] string? languageCode,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListEmailTemplatesQuery(Name: name, LanguageCode: languageCode),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListEmailTemplates")
            .WithSummary("List email templates")
            .WithDescription("SuperAdmin. Optional filters: name, languageCode (tr|en).")
            .Produces<ListEmailTemplatesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetEmailTemplateQuery(id), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetEmailTemplate")
            .WithSummary("Get email template by id")
            .Produces<EmailTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateEmailTemplateBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new UpdateEmailTemplateCommand(id, body.Subject, body.Body),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateEmailTemplate")
            .WithSummary("Update email template subject/body")
            .WithDescription("SuperAdmin. Name and languageCode are immutable (unique key).")
            .Produces<EmailTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/preview", async (
                Guid id,
                [FromBody] PreviewEmailTemplateBody? body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new PreviewEmailTemplateCommand(id, body?.Tokens),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("PreviewEmailTemplate")
            .WithSummary("Preview rendered template")
            .WithDescription("Renders with sample tokens (optional overrides). No SMTP send.")
            .Produces<PreviewEmailTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/test-send", async (
                Guid id,
                [FromBody] TestSendEmailTemplateBody? body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new TestSendEmailTemplateCommand(
                        Id: id,
                        ToEmail: body?.ToEmail,
                        Tokens: body?.Tokens),
                    cancellationToken);
                return result.MapResult(
                    () => Results.NoContent(),
                    httpContext.Request.Path.Value);
            })
            .WithName("TestSendEmailTemplate")
            .WithSummary("Send test email for template")
            .WithDescription(
                "SuperAdmin. Renders sample (or override tokens) and sends via SMTP. " +
                "Optional toEmail (default: SuperAdmin). SET_003 if SMTP off.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public sealed record UpdateEmailTemplateBody(string Subject, string Body);

    public sealed record PreviewEmailTemplateBody(
        IReadOnlyDictionary<string, string>? Tokens = null);

    public sealed record TestSendEmailTemplateBody(
        string? ToEmail = null,
        IReadOnlyDictionary<string, string>? Tokens = null);
}
