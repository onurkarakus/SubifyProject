namespace Subify.Api.Features.Categories.CreateCategories;

using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions; // For MapResult and ToProblemDetails
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Subify.Domain.Shared;
using Microsoft.AspNetCore.Http; // For StatusCodes

public class CreateCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/categories", async (
            [FromBody] CreateCategoryCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.MapResult(
                onSuccess: response => Results.Created(
                    uri: $"/api/categories/{response.Id}", // Manually construct URI
                    value: response),
                onFailure: result => result.Error.Type == ErrorType.Conflict
                    ? Results.Conflict(result.ToProblemDetails())
                    : Results.BadRequest(result.ToProblemDetails()));
        })
        .WithName("CreateCategory")
        .WithTags("Categories")
        .Produces<CreateCategoryResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest) // Validation errors or other bad requests
        .ProducesProblem(StatusCodes.Status409Conflict); // Duplicate slug
    }
}
