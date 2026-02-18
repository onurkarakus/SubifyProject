using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Categories.CreateCategories;
using Subify.Api.Features.Categories.DeleteCategory;
using Subify.Api.Features.Categories.GetCategories;
using Subify.Api.Features.Categories.UpdateCategories;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories;

public class CategoriesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization("Admin")
            .WithOpenApi();

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCategoryQuery());

            return result.MapResult(
                onSuccess: categories => Results.Ok(categories),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Get Current Categories")
            .WithSummary("Get all categories.")
            .WithDescription("Retrieves the list of categories.")
            .Produces<List<GetCategoryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}", async (ISender sender, Guid id, [FromBody] CreateCategoryCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
               onSuccess: categories => Results.Ok(categories),
               onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .WithName("Create Category")
            .WithSummary("Creates a new category.")
            .WithDescription("Creates a new category with the provided details.")
            .Produces<CreateCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapDelete("/{id}", async ([FromServices] ISender sender, [FromRoute] Guid id) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id));

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails())
        );
        })
            .WithName("Delete Category")
            .WithSummary("Deletes a category by its ID.")
            .WithDescription("Deletes a category by its ID. Requires admin privileges.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id}", async (Guid id, UpdateCategoryCommand command, ISender sender) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Update Category")
            .WithSummary("Update Category")
            .WithDescription("Updates an existing category. URL'deki ID ile Body'deki ID'nin eşleşmesi gerekir.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
