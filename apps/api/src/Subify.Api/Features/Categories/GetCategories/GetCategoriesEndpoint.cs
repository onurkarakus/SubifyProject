using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Categories.GetCategories;

public class GetCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCategoriesQuery());

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }
            
            return Results.Ok(result.Value);
        })
            .WithTags("Categories")
        .WithName("GetCurrentCategories")
        .WithSummary("Get all categories.")
        .WithDescription("Retrieves the list of categories.")
        .Produces<List<GetCategoriesResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .WithOpenApi();
    }
}
