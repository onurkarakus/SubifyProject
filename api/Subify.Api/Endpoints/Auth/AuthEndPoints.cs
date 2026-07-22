using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.RateLimiting;
using Subify.Application.Features.Auth.AdminResetPassword;
using Subify.Application.Features.Auth.ChangePassword;
using Subify.Application.Features.Auth.Login;
using Subify.Application.Features.Auth.Logout;
using Subify.Application.Features.Auth.Refresh;
using Subify.Application.Features.Auth.Register;
using Subify.Infrastructure.Authorization;

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
                    onSuccess: r => Results.Ok(r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Login")
            .WithSummary("User login")
            .WithDescription(
                "Email/password → tokens + user summary. No EmailConfirmed check. " +
                "401 AUTH_001 invalid credentials; 423 AUTH_005 lockout.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();

        MapRefreshEndpoint(group, "/refresh-token", "RefreshToken");
        MapRefreshEndpoint(group, "/refresh", "RefreshTokenAlias");

        group.MapPost("/logout", async (
                [FromBody] LogoutCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Logout")
            .WithSummary("Logout / revoke refresh token")
            .WithDescription(
                "Body: { refreshToken } and/or { allSessions: true }. " +
                "Revokes with reason logout. Idempotent for unknown tokens. Task 3.2.4.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous(); // single-token logout without access JWT

        group.MapPost("/register", async (
                [FromBody] RegisterCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Created($"/api/users/{r.UserId}", r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Register")
            .WithSummary("User registration")
            .WithDescription(
                "Creates User + NotificationSettings. EmailConfirmed=true. " +
                "Blocked when setup incomplete or AllowPublicRegistration=false (403 AUTH_014). " +
                "Duplicate email 409 AUTH_008.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.RegisterPolicy)
            .AllowAnonymous();

        group.MapPost("/change-password", async (
                [FromBody] ChangePasswordCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("ChangePassword")
            .WithSummary("Change own password")
            .WithDescription("Requires auth. currentPassword + newPassword. Revokes all refresh sessions. Task 3.2.14.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.Authenticated);

        // Admin reset lives under /api/admin (task 3.2.15)
        var admin = app.MapGroup("/api/admin")
            .WithTags("Admin");

        admin.MapPost("/users/{id:guid}/reset-password", async (
                Guid id,
                [FromBody] AdminResetPasswordBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new AdminResetPasswordCommand(id, body.NewPassword),
                    cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("AdminResetPassword")
            .WithSummary("SuperAdmin reset user password")
            .WithDescription("Sets a new password without email. Revokes target user sessions. Task 3.2.15.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.SuperAdmin);
    }

    private static void MapRefreshEndpoint(RouteGroupBuilder group, string route, string name)
    {
        group.MapPost(route, async (
                [FromBody] RefreshCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: response => Results.Ok(response),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName(name)
            .WithSummary("Refresh tokens (rotate)")
            .WithDescription(
                "Body: { refreshToken }. New access+refresh; old replaced. Reuse → AUTH_016. Tasks 3.1.3 / 3.2.3.")
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();
    }

    public sealed record AdminResetPasswordBody(string NewPassword);
}
