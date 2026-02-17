using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.UserCategories.DeleteUserCategory;

public class DeleteUserCategoryEndpoint: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/user-categories/", async (ISender sender, [FromBody] DeleteUserCategoryCommand deleteUserCategoryCommand) =>
        {
            var result = await sender.Send(deleteUserCategoryCommand);

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .RequireAuthorization()
            .WithTags("User Categories")
            .WithName("DeleteUserCategory")
            .WithSummary("Deletes an existing user category.")
            .WithDescription("Deletes an existing user category by its ID.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
