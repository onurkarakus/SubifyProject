using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.RefreshTokens;

public class RefreshTokensEndpoint: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refreshTokens", async ([FromBody] RefreshTokensCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: refreshTokensResponse => Results.Ok(refreshTokensResponse),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithTags("Auth")
            .WithName("RefreshToken")
            .WithSummary("Refresh authentication tokens.")
            .WithDescription("Refreshes the access and refresh tokens using the provided tokens.")
            .Produces<RefreshTokensResponse>(StatusCodes.Status200OK) // Changed to RefreshTokensResponse
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}
