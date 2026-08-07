using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Activity;
using Subify.Application.Features.Activity.ListActivity;
using Subify.Domain.Constants;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Activity;

/// <summary>User activity feed (Faz 5.4).</summary>
public sealed class ActivityEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activity")
            .WithTags("Activity")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 5.4.2
        group.MapGet("/", async (
                [FromQuery] string? entityType,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListActivityQuery(
                        EntityType: entityType,
                        Page: page ?? SubscriptionConstants.DefaultPage,
                        PageSize: pageSize ?? 10),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListActivity")
            .WithSummary("List my activity")
            .WithDescription(
                "Own activity only. Optional entityType filter (e.g. Subscription, Profile). " +
                "Newest first. Pagination page/pageSize.")
            .Produces<ListActivityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
