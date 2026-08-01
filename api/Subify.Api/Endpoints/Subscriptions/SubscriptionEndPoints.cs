using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Subscriptions;
using Subify.Application.Features.Subscriptions.ArchiveSubscription;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Application.Features.Subscriptions.GetSubscriptionById;
using Subify.Application.Features.Subscriptions.ListSubscriptions;
using Subify.Application.Features.Subscriptions.ReactivateSubscription;
using Subify.Application.Features.Subscriptions.UpcomingSubscriptions;
using Subify.Application.Features.Subscriptions.UpdateSubscription;
using Subify.Domain.Constants;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Subscriptions;

/// <summary>Subscription CRUD + upcoming (Faz 4.2).</summary>
public sealed class SubscriptionEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions")
            .WithTags("Subscriptions")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 4.2.1
        group.MapGet("/", async (
                [FromQuery] bool? includeArchived,
                [FromQuery] string? category,
                [FromQuery] Guid? categoryId,
                [FromQuery] Guid? userCategoryId,
                [FromQuery] string? search,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListSubscriptionsQuery(
                        IncludeArchived: includeArchived ?? false,
                        Category: category,
                        CategoryId: categoryId,
                        UserCategoryId: userCategoryId,
                        Search: search,
                        Page: page ?? SubscriptionConstants.DefaultPage,
                        PageSize: pageSize ?? SubscriptionConstants.DefaultPageSize),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListSubscriptions")
            .WithSummary("List subscriptions")
            .WithDescription(
                "Current user only. Filters: includeArchived, category (slug), categoryId, " +
                "userCategoryId, search. Pagination page/pageSize. Summary uses MainCurrency.")
            .Produces<ListSubscriptionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 4.2.6 — before {id} for clarity (guid constraint already avoids clash)
        group.MapGet("/upcoming", async (
                [FromQuery] int? days,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new UpcomingSubscriptionsQuery(
                        Days: days ?? SubscriptionConstants.DefaultUpcomingDays),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpcomingSubscriptions")
            .WithSummary("Upcoming and overdue renewals")
            .WithDescription(
                "Active subscriptions with nextRenewal within days window or overdue. " +
                "Each item: daysUntilRenewal, isOverdue, isUpcoming.")
            .Produces<UpcomingSubscriptionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 4.2.2
        group.MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetSubscriptionByIdQuery(id), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetSubscriptionById")
            .WithSummary("Get subscription by id")
            .WithDescription("Ownership enforced. 404 SUB_001, 403 SUB_002. Nested category/provider.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 4.2.3
        group.MapPost("/", async (
                [FromBody] CreateSubscriptionCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/subscriptions/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateSubscription")
            .WithSummary("Create subscription")
            .WithDescription("No freemium limit. Provider must be active. Category XOR userCategory.")
            .Produces<CreateSubscriptionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 4.2.4
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateSubscriptionBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var command = body.ToCommand(id);
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateSubscription")
            .WithSummary("Update subscription")
            .WithDescription("Ownership required. Writes subscription.updated activity.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 4.2.5
        group.MapDelete("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new ArchiveSubscriptionCommand(id), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ArchiveSubscription")
            .WithSummary("Archive subscription (soft delete)")
            .WithDescription("Sets Archived=true. Idempotent. Activity subscription.archived.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 4.1.8 helper endpoint (not in 4.2 checklist but completes archive cycle)
        group.MapPost("/{id:guid}/reactivate", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new ReactivateSubscriptionCommand(id), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ReactivateSubscription")
            .WithSummary("Reactivate archived subscription")
            .WithDescription("Clears archive. Idempotent when already active.")
            .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <summary>PUT body without id (id comes from route).</summary>
    public sealed record UpdateSubscriptionBody(
        string Name,
        decimal Price,
        string Currency,
        string BillingCycle,
        int SharedWithCount,
        DateOnly NextRenewalDate,
        Guid? ProviderId = null,
        Guid? CategoryId = null,
        Guid? UserCategoryId = null,
        DateOnly? LastUsedAt = null,
        string? Notes = null)
    {
        public UpdateSubscriptionCommand ToCommand(Guid id) =>
            new(
                Id: id,
                Name: Name,
                Price: Price,
                Currency: Currency,
                BillingCycle: BillingCycle,
                SharedWithCount: SharedWithCount,
                NextRenewalDate: NextRenewalDate,
                ProviderId: ProviderId,
                CategoryId: CategoryId,
                UserCategoryId: UserCategoryId,
                LastUsedAt: LastUsedAt,
                Notes: Notes);
    }
}
