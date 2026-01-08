using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Auth.Login;

namespace Subify.Api.Features.Auth.RefreshTokens;

public class RefreshTokensEndpoint: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refreshTokens", async ([FromBody] RefreshTokensCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }

            return Results.Ok(result.Value);
        })
            .WithTags("Auth")
            .WithName("RefreshToken")
            .WithSummary("Refresh authentication tokens.")
            .WithDescription("Refreshes the access and refresh tokens using the provided tokens.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}
