using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Admin.Settings;
using Subify.Application.Features.Admin.Settings.GetSystemSettings;
using Subify.Application.Features.Admin.Settings.TestAi;
using Subify.Application.Features.Admin.Settings.TestSmtp;
using Subify.Application.Features.Admin.Settings.UpdateSystemSettings;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Admin;

/// <summary>SystemSettings admin API (Faz 7.3: GET/PUT, test-smtp, test-ai).</summary>
public sealed class AdminSettingsEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/settings")
            .WithTags("Admin · Settings")
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        // 7.3.1
        group.MapGet("/", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetSystemSettingsQuery(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetSystemSettings")
            .WithSummary("Get system settings")
            .WithDescription(
                "SuperAdmin only. Instance + AI + SMTP. " +
                "AI API key and SMTP password are masked (hasApiKey / hasPassword + mask).")
            .Produces<SystemSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 7.3.2
        group.MapPut("/", async (
                [FromBody] UpdateSystemSettingsCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateSystemSettings")
            .WithSummary("Update system settings")
            .WithDescription(
                "SuperAdmin only. Partial update for instance defaults, AI, SMTP. " +
                "Secrets: omit/null = leave unchanged; empty string = clear; non-empty = set. " +
                "Writes activity audit without secret values (7.3.5).")
            .Produces<SystemSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 15.3.3 / 7.3.3
        group.MapPost("/test-smtp", async (
                [FromBody] TestSmtpCommand? command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command ?? new TestSmtpCommand(), cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("TestSmtp")
            .WithSummary("Send SMTP test email")
            .WithDescription(
                "SuperAdmin only. Optional body { toEmail }. Defaults to current SuperAdmin email. " +
                "SET_003 if SMTP not configured; SET_004 on send failure.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 7.3.4
        group.MapPost("/test-ai", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new TestAiCommand(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("TestAi")
            .WithSummary("Ping configured LLM (BYOK)")
            .WithDescription(
                "SuperAdmin only. Minimal chat completion with stored AI key/model. " +
                "Returns model, latency, reply preview. " +
                "AI_KEY_MISSING when key unset; AI_004/AI_005 on provider errors. " +
                "Does not use user AI rate-limit quotas.")
            .Produces<TestAiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
