using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Resources;
using Subify.Application.Features.Resources.GetResources;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Resources;

/// <summary>Client i18n resource pack (Faz 6.3.1–6.3.2).</summary>
public sealed class ResourceEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resources")
            .WithTags("Resources")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 6.3.1
        group.MapGet("/", async (
                [FromQuery] string? lang,
                [FromQuery] DateTimeOffset? since,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetResourcesQuery(
                        Lang: lang,
                        Since: since,
                        AcceptLanguage: httpContext.Request.Headers.AcceptLanguage.ToString()),
                    cancellationToken);

                if (result.IsSuccess && result.Value.NotModified)
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetResources")
            .WithSummary("Localization resources (delta sync)")
            .WithDescription(
                "Full pack when since omitted (memory-cached). " +
                "With since=ISO8601 only rows changed after that timestamp. " +
                "HTTP 304 when nothing changed.")
            .Produces<ListResourcesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
