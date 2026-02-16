using MediatR;
using Microsoft.AspNetCore.Authorization;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Categories.GetCategories;

public class GetCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCategoryQuery());

            return result.MapResult(
                onSuccess: categories => Results.Ok(categories),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .RequireAuthorization("Admin")
            .WithTags("Categories")
            .WithName("GetCurrentCategories")
            .WithSummary("Get all categories.")
            .WithDescription("Retrieves the list of categories.")
            .Produces<List<GetCategoryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
