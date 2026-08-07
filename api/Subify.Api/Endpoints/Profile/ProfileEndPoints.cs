using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Profile;
using Subify.Application.Features.Profile.GetNotificationSettings;
using Subify.Application.Features.Profile.GetProfile;
using Subify.Application.Features.Profile.UpdateNotificationSettings;
using Subify.Application.Features.Profile.UpdateProfile;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Profile;

/// <summary>User profile preferences (Faz 5.3).</summary>
public sealed class ProfileEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 5.3.1
        group.MapGet("/", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetProfileQuery(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetProfile")
            .WithSummary("Get current user profile")
            .WithDescription(
                "Email + preferences (locale, currency, budget, theme). " +
                "No plan/premium fields (Subify OS).")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 5.3.2 (+ 5.3.3 theme whitelist, 5.3.4 currency set)
        group.MapPut("/", async (
                [FromBody] UpdateProfileCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateProfile")
            .WithSummary("Update current user profile")
            .WithDescription(
                "fullName, locale (tr|en), mainCurrency (TRY|USD|EUR|GBP), monthlyBudget (null to clear), " +
                "applicationThemeColor (preset list), darkTheme. Returns updated profile.")
            .Produces<ProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 5.3.5
        group.MapGet("/notifications", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetNotificationSettingsQuery(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetNotificationSettings")
            .WithSummary("Get notification preferences")
            .Produces<NotificationSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/notifications", async (
                [FromBody] UpdateNotificationSettingsCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateNotificationSettings")
            .WithSummary("Update notification preferences")
            .WithDescription(
                "daysBeforeRenewal (0–30) for in-app renewal hints and email window. " +
                "emailEnabled: when true and instance SMTP is configured, renewal reminder emails may send. " +
                "pushEnabled optional (client preference).")
            .Produces<NotificationSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
