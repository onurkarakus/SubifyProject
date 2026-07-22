using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Setup.CompleteSetup;
using Subify.Application.Features.Setup.CreateSetupAdmin;
using Subify.Application.Features.Setup.GetSetupStatus;
using Subify.Application.Features.Setup.UpdateSetupAi;
using Subify.Application.Features.Setup.UpdateSetupInstance;
using Subify.Application.Features.Setup.UpdateSetupSmtp;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Setup;

/// <summary>First-run setup wizard API (Faz 3S).</summary>
public sealed class SetupEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("Setup");

        group.MapGet("/status", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetSetupStatusQuery(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetSetupStatus")
            .WithSummary("Setup status (public)")
            .WithDescription("No secrets. Web uses this for redirect to /setup.")
            .Produces<SetupStatusResponse>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapPost("/admin", async (
                [FromBody] CreateSetupAdminCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/users/{r.UserId}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateSetupAdmin")
            .WithSummary("Create first Super Admin")
            .WithDescription("Only while setup incomplete and no SuperAdmin. Returns tokens for wizard (3S.2.2).")
            .Produces<CreateSetupAdminResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapPut("/instance", async (
                [FromBody] UpdateSetupInstanceCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("UpdateSetupInstance")
            .WithSummary("Setup: instance defaults")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapPut("/smtp", async (
                [FromBody] UpdateSetupSmtpCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("UpdateSetupSmtp")
            .WithSummary("Setup: save SMTP (no send)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapPut("/ai", async (
                [FromBody] UpdateSetupAiCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("UpdateSetupAi")
            .WithSummary("Setup: save AI BYOK key")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapPost("/complete", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new CompleteSetupCommand(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("CompleteSetup")
            .WithSummary("Finish setup wizard")
            .WithDescription("Requires SuperAdmin. Sets IsSetupComplete=true. Repeat → 409 SETUP_001.")
            .Produces<CompleteSetupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.SuperAdmin);
    }
}
