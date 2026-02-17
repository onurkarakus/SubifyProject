using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.UserCategories.CreateUserCategory;

namespace Subify.Api.Features.UserCategories.UpdateUserCategory;

public class UpdateUserCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/user-categories/{id}", async (ISender sender, [FromQuery] Guid id, [FromBody] UpdateUserCategoryCommand command) =>
        {
            if (id != command.Id) return Results.BadRequest("The ID in the URL does not match the ID in the request body.");

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .RequireAuthorization()
            .WithTags("User Categories")
            .WithName("UpdateUserCategory")
            .WithSummary("Updates an existing user category.")
            .WithDescription("Updates an existing user category. The ID in the URL must match the ID in the request body.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
