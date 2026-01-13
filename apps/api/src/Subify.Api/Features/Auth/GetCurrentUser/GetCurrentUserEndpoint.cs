using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using System.Security.Claims;

namespace Subify.Api.Features.Auth.GetCurrentUser;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (ISender sender, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var command = new GetCurrentUserQuery(userId);
            var result = await sender.Send(command);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetail();
        })
            .WithTags("Auth")
            .WithName("GetCurrentUser")
            .WithSummary("Get the current authenticated user.")
            .WithDescription("Retrieves information about the currently authenticated user.")
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}
