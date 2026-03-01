using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Subscriptions.CreateSubscription;
using Subify.Api.Features.Subscriptions.GetSubscriptions;

namespace Subify.Api.Features.Subscriptions;

public class SubscriptionsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions")
                    .WithTags("Subscriptions")
                    .RequireAuthorization()
                    .WithOpenApi();

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetSubscriptionsQuery());

            return result.MapResult(
                onSuccess: categories => Results.Ok(categories),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
           .WithName("Get Current Users Subscriptions")
           .WithSummary("Get all users subscriptions.")
           .WithDescription("Retrieves the list of users subscriptions.")
           .Produces<List<GetSubscriptionsResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", async (ISender sender, [FromBody] CreateSubscriptionCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: success => Results.Ok(result),
                onFailure: failure => Results.Problem(failure.ToProblemDetails())
            );
        })
            .WithName("CreateSubscription")
            .WithSummary("Create a new subscription")
            .WithDescription("Create a new subscription")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
