using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.UserCategories.GetUserCategories;

namespace Subify.Api.Features.UserCategories.CreateUserCategory;

public class CreateUserCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/user-categories/", async (ISender sender, [FromBody] CreateUserCategoryCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: createdUserId => Results.Ok(createdUserId),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .RequireAuthorization()
            .WithTags("User Categories")
            .WithName("CreateUserCategory")
            .WithSummary("Creates a new user category.")
            .WithDescription("Creates a new user category with the provided name, description, icon, and color.")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
