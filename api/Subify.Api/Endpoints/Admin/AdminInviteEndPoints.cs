using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Admin.Invites;
using Subify.Application.Features.Admin.Invites.CreateInvite;
using Subify.Application.Features.Admin.Invites.ListInvites;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Admin;

/// <summary>Admin invites (Faz 7.2.1–7.2.2). Optional email when SMTP configured.</summary>
public sealed class AdminInviteEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/invites")
            .WithTags("Admin · Invites")
            .RequireAuthorization(AuthPolicies.AdminOrAbove);

        // 7.2.1
        group.MapPost("/", async (
                [FromBody] CreateInviteCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/admin/invites/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateInvite")
            .WithSummary("Create user invite")
            .WithDescription(
                "SuperAdmin/Admin. Email + optional expiryDays (1–90, default 7). " +
                "Response includes plain token, inviteUrl, and emailSent. " +
                "When sendEmail=true (default) and SMTP is configured, invite mail is sent; create still succeeds if send fails.")
            .Produces<CreateInviteResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 7.2.2
        group.MapGet("/", async (
                [FromQuery] bool? includeExpired,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListInvitesQuery(IncludeExpired: includeExpired ?? false),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListInvites")
            .WithSummary("List pending invites")
            .WithDescription(
                "SuperAdmin/Admin. Default: pending only (not used, not expired). " +
                "includeExpired=true also lists unused expired. Never returns plain tokens.")
            .Produces<ListInvitesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
