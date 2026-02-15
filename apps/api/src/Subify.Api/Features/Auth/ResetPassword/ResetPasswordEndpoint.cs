using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.ResetPassword;

public class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", async ([FromBody] ResetPasswordCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "Şifreniz başarıyla güncellendi." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithTags("Auth")
            .WithName("ResetPassword")
            .WithSummary("Resets a user's password.")
            .WithDescription("Resets the password for a user using the provided email, token, and new password.")
            .Produces<object>(StatusCodes.Status200OK) // Change to object as per API_CONTRACTS.md
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}
