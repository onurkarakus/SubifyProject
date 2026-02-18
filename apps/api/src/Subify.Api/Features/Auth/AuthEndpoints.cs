using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Auth.ForgotPassword;
using Subify.Api.Features.Auth.GetCurrentUser;
using Subify.Api.Features.Auth.Login;
using Subify.Api.Features.Auth.Logout;
using Subify.Api.Features.Auth.RefreshTokens;
using Subify.Api.Features.Auth.Register;
using Subify.Api.Features.Auth.ResendConfirmation;
using Subify.Api.Features.Auth.ResetPassword;
using Subify.Api.Features.Auth.VerifyEmail;
using Subify.Domain.Shared;
using System.Security.Claims;

namespace Subify.Api.Features.Auth;

public class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .WithOpenApi();

        group.MapPost("/forgot-password", async (ISender sender, [FromBody] ForgotPasswordCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "Şifre sıfırlama linki e-posta adresinize gönderildi." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
        .WithName("Forgot Password")
        .WithSummary("Forgot Password")
        .WithDescription("Sends a password reset link to the user's email address.");

        group.MapGet("/me", async (ISender sender, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var command = new GetCurrentUserQuery(userId);
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: currentUser => Results.Ok(currentUser),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Get Current User")
            .WithSummary("Get the current authenticated user.")
            .WithDescription("Retrieves information about the currently authenticated user.")
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

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

        group.MapPost("/logout", async (ISender sender, [FromBody] LogoutCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "Başarıyla çıkış yapıldı." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
        .WithName("Logout")
        .WithDescription("Logs out the user by revoking the provided refresh token.")
        .WithSummary("Logs out the user by revoking the provided refresh token.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/refreshTokens", async ([FromBody] RefreshTokensCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: refreshTokensResponse => Results.Ok(refreshTokensResponse),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Refresh Token")
            .WithSummary("Refresh authentication tokens.")
            .WithDescription("Refreshes the access and refresh tokens using the provided tokens.")
            .Produces<RefreshTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", async ([FromBody] RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: registerResponse => Results.Created(
                    uri: $"/api/profile/{registerResponse.UserId}",
                    value: new { message = "Kayıt başarılı. Lütfen e-postanızı doğrulayın.", userId = registerResponse.UserId }
                ),
                onFailure: result => result.Error.Type == ErrorType.Conflict
                    ? Results.Conflict(result.ToProblemDetails())
                    : Results.BadRequest(result.ToProblemDetails())
            );
        })
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Creates a new user account with the provided email, password, and full name.")
            .Produces<object>(StatusCodes.Status201Created) // Changed to object, 201 Created
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/resend-confirmation", async ([FromBody] ResendConfirmationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "E-posta doğrulama linki tekrar gönderildi." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Resend Confirmation")
            .WithSummary("Resends the email confirmation link to the user's email address.")
            .WithDescription("Sends a new email confirmation link to the specified email address if the user exists and their email is not yet confirmed.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/reset-password", async ([FromBody] ResetPasswordCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "Şifreniz başarıyla güncellendi." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
            .WithName("Reset Password")
            .WithSummary("Resets a user's password.")
            .WithDescription("Resets the password for a user using the provided email, token, and new password.")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/verify-email", async (ISender sender, [FromBody] VerifyEmailCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "E-posta başarıyla doğrulandı." }),
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
        .WithName("Verify Email")
        .WithSummary("Verifies a user's email address.")
        .WithDescription("Verifies the email address of a user using the provided user ID and verification token.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
