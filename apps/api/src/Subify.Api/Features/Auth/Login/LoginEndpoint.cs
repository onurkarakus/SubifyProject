using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.Login;

public class LoginEndpoint: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async ([FromBody] LoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }

            return Results.Ok(result.Value);
        })
            .WithTags("Auth")
            .WithName("Login")
            .WithSummary("Login a new user.")
            .WithDescription("Logs in a user with the provided email and password.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();
    }
}
