using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.RateLimiting;
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
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);

                return result.MapResult(
                    onSuccess: loginResponse => Results.Ok(loginResponse),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Login")
            .WithSummary("User login")
            .WithDescription("Authenticates a user and returns access + refresh tokens.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();

        group.MapPost("/register", async (
                [FromBody] RegisterCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);

                return result.MapResult(
                    onSuccess: response => Results.Created($"/api/users/{response.UserId}", response),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Register")
            .WithSummary("User registration")
            .WithDescription("Registers a new user account.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.RegisterPolicy)
            .AllowAnonymous();
    }
}

