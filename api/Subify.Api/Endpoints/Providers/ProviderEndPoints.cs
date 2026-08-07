using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Providers;
using Subify.Application.Features.Providers.GetProviderById;
using Subify.Application.Features.Providers.ListProviders;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Providers;

/// <summary>Provider catalog (Faz 5.2).</summary>
public sealed class ProviderEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/providers")
            .WithTags("Providers")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 5.2.1
        group.MapGet("/", async (
                [FromQuery] string? search,
                [FromQuery] string? region,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListProvidersQuery(Search: search, Region: region),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListProviders")
            .WithSummary("List active providers")
            .WithDescription(
                "Catalog providers with IsActive=true. Optional search (name/slug) and region " +
                "(exact region or GLOBAL providers). Ordered by name.")
            .Produces<ListProvidersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 5.2.2
        group.MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetProviderByIdQuery(id), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetProviderById")
            .WithSummary("Get provider by id")
            .WithDescription("Catalog provider detail. Missing/soft-deleted → 404 PROV_001.")
            .Produces<ProviderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
