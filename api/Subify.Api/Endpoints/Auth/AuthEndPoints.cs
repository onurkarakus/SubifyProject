using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Auth.Login;

namespace Subify.Api.Endpoints.Auth
{
    public class AuthEndPoints: IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

            group.MapPost("/login", async ([FromBody] LoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: loginResponse => Results.Ok(loginResponse),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Login")
            .WithSummary("Login a new user.")
            .WithDescription("Logs in a user with the provided email and password.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }
}