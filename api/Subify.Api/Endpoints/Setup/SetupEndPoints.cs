using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Setup.CreateSetupAdmin;
using Subify.Application.Features.Setup.GetSetupStatus;

namespace Subify.Api.Endpoints.Setup;

/// <summary>First-run setup API surface (3.3.1 / 3.3.6 / 3S.1).</summary>
public sealed class SetupEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("Setup")
            .AllowAnonymous();

        group.MapGet("/status", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetSetupStatusQuery(), cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Ok(r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("GetSetupStatus")
            .WithSummary("Setup status (public)")
            .WithDescription("isSetupComplete, hasSuperAdmin, allowPublicRegistration — no secrets.")
            .Produces<SetupStatusResponse>(StatusCodes.Status200OK);

        group.MapPost("/admin", async (
                [FromBody] CreateSetupAdminCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Created($"/api/users/{r.UserId}", r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("CreateSetupAdmin")
            .WithSummary("Create first Super Admin")
            .WithDescription(
                "Race-safe SuperAdmin bootstrap while setup is incomplete. " +
                "Fails if SuperAdmin already exists or setup is complete. Task 3.3.1 / 3.3.6.")
            .Produces<CreateSetupAdminResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
