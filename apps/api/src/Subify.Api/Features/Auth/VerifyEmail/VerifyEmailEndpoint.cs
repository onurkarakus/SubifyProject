using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.VerifyEmail;

public class VerifyEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/verify-email", async (ISender sender, [FromBody] VerifyEmailCommand command) =>
        {
            var result = await sender.Send(command);

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }

            return Results.Ok(new { message = "E-posta başarıyla doğrulandı." });
        })
        .WithTags("Auth")
        .WithName("VerifyEmail")
        .WithSummary("Verifies a user's email address.")
        .WithDescription("Verifies the email address of a user using the provided user ID and verification token.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();
    }
}