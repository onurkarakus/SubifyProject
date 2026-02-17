using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Categories.UpdateCategories;

public class UpdateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/categories/{id}", async (Guid id, UpdateCategoryCommand command, ISender sender) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(
                    Result.Failure(DomainErrors.ValidationErrors.ValidationFailed)
                    .ToProblemDetails()
                );
            }

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(Result.Success()),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .RequireAuthorization("Admin")
            .WithTags("Categories")
            .WithName("UpdateCategory")
            .WithSummary("Update Category")
            .WithDescription("Updates an existing category. URL'deki ID ile Body'deki ID'nin eşleşmesi gerekir.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}