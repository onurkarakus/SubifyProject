using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Categories.DeleteCategory;

public class DeleteCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/categories/{id}", async ([FromServices] ISender sender, [FromRoute] Guid id) =>
            {
                var result = await sender.Send(new DeleteCategoryCommand(id));

                return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );

            })
            .RequireAuthorization("Admin")
            .WithTags("Categories")
            .WithName(nameof(DeleteCategoryEndpoint))
            .WithSummary("Deletes a category by its ID.")
            .WithDescription("Deletes a category by its ID. Requires admin privileges.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithOpenApi();
    }
}