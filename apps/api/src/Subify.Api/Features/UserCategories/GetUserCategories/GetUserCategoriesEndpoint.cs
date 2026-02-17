using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Categories.GetCategories;

namespace Subify.Api.Features.UserCategories.GetUserCategories;

public class GetUserCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-categories/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserCategoriesQuery());

            return result.MapResult(
                onSuccess: categories => Results.Ok(categories),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .RequireAuthorization()
            .WithTags("User Categories")
            .WithName("GetUserCategories")
            .WithSummary("Get all user categories")
            .WithDescription("Get all user categories for the authenticated user")
            .Produces<List<GetUserCategoryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();
    }
}
