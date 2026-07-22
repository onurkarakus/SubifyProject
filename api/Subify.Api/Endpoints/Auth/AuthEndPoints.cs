using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Auth.Login;
using Subify.Application.Features.Auth.Register;

namespace Subify.Api.Endpoints.Auth;

public class AuthEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
                [FromBody] LoginCommand command,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);

                return result.MapResult(
                    onSuccess: loginResponse => Results.Ok(loginResponse),
                    onFailure: failure => Results.Problem(failure.ToProblemDetails()));
            })
            .WithName("Login")
            .WithSummary("User login")
            .WithDescription("Authenticates a user and returns access + refresh tokens.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/register", async (
                [FromBody] RegisterCommand command,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);

                return result.MapResult(
                    onSuccess: response => Results.Created($"/api/users/{response.UserId}", response),
                    onFailure: failure => Results.Problem(failure.ToProblemDetails()));
            })
            .WithName("Register")
            .WithSummary("User registration")
            .WithDescription("Registers a new user account.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();
    }
}
