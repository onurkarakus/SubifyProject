using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.Register;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async ([FromBody] RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: registerResponse => Results.Created(
                    uri: $"/api/profile/{registerResponse.UserId}", // Assuming a /api/profile/{id} route will exist for GET
                    value: new { message = "Kayıt başarılı. Lütfen e-postanızı doğrulayın.", userId = registerResponse.UserId }
                ),
                onFailure: result => result.Error.Type == ErrorType.Conflict
                    ? Results.Conflict(result.ToProblemDetails())
                    : Results.BadRequest(result.ToProblemDetails())
            );
        })
            .WithTags("Auth")
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Creates a new user account with the provided email, password, and full name.")
            .Produces<object>(StatusCodes.Status201Created) // Changed to object, 201 Created
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();
    }
}
