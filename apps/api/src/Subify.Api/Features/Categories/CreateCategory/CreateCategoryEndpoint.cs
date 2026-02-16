
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Categories.CreateCategories;
public class CreateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id}", async (ISender sender, Guid id, [FromBody] CreateCategoryCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
               onSuccess: categories => Results.Ok(categories),
               onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .WithTags("Categories")
            .WithName(nameof(CreateCategoryEndpoint))
            .WithSummary("Creates a new category.")
            .WithDescription("Creates a new category with the provided details.")
            .Produces<CreateCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithOpenApi();
    }
}
