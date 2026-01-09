using MediatR;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.GetCurrentUser;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCurrentUserQuery());
            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }
            return Results.Ok(result.Value);
        })
            .WithTags("Auth")
            .WithName("GetCurrentUser")
            .WithSummary("Get the current authenticated user.")
            .WithDescription("Retrieves information about the currently authenticated user.")
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();)
    }
}
