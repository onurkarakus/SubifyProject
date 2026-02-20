using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Providers.CreateProvider;
using Subify.Api.Features.Providers.DeleteProvider;
using Subify.Api.Features.Providers.GetProviders;
using Subify.Api.Features.Providers.UpdateProvider;

namespace Subify.Api.Features.Providers;

public class ProvidersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/providers")
            .WithTags("Providers")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetProvidersQuery());

            return result.MapResult(
                onSuccess: success => Results.Ok(result),
                onFailure: failure => Results.Problem(failure.ToProblemDetails())
            );
        })
            .WithName("GetProviders")
            .WithSummary("Get all providers")
            .WithDescription("Get all providers")
            .Produces<List<GetProviderResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/", async (ISender sender, [FromBody] CreateProviderCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: success => Results.Ok(result),
                onFailure: failure => Results.Problem(failure.ToProblemDetails())
            );
        })
            .RequireAuthorization("Admin")
            .WithName("CreateProvider")
            .WithSummary("Create a new provider")
            .WithDescription("Create a new provider")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id}", async ([FromServices] ISender sender, [FromQuery] Guid id, [FromBody] UpdateProviderCommand command) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(result),
                onFailure: failure => Results.Problem(failure.ToProblemDetails())
            );

        })
        .RequireAuthorization("Admin")
        .WithName("UpdateProvider")
        .WithSummary("Update an existing provider")
        .WithDescription("Update an existing provider")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}", async (ISender sender, Guid id) =>
        {
            var result = await sender.Send(new DeleteProviderCommand(id));
            
            return result.MapResult(
                onSuccess: () => Results.Ok(result),
                onFailure: failure => Results.Problem(failure.ToProblemDetails())
            );
        })        
        .RequireAuthorization("Admin")
        .WithName("DeleteProvider")
        .WithSummary("Delete a provider")
        .WithDescription("Delete a provider")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError);



    }
}
