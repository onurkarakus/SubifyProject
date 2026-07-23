using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionHandler
    : IRequestHandler<CreateSubscriptionCommand, Result<CreateSubscriptionResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateSubscriptionHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<CreateSubscriptionResponse>> Handle(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<CreateSubscriptionResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        if (!CreateSubscriptionValidator.TryParseBillingCycle(request.BillingCycle, out var billingCycle))
        {
            return Result.Failure<CreateSubscriptionResponse>(DomainErrors.Subscription.InvalidBillingCycle);
        }

        var refs = await SubscriptionReferenceValidator.ValidateAsync(
            _db,
            userId,
            request.ProviderId,
            request.CategoryId,
            request.UserCategoryId,
            cancellationToken);

        if (refs.IsFailure)
        {
            return Result.Failure<CreateSubscriptionResponse>(refs.Error);
        }

        var create = Subscription.Create(
            userId: userId,
            name: request.Name,
            price: request.Price,
            currency: request.Currency,
            billingCycle: billingCycle,
            sharedWithCount: request.SharedWithCount,
            nextRenewalDate: request.NextRenewalDate,
            providerId: request.ProviderId,
            categoryId: request.CategoryId,
            userCategoryId: request.UserCategoryId,
            lastUsedAt: request.LastUsedAt,
            notes: request.Notes);

        if (create.IsFailure)
        {
            return Result.Failure<CreateSubscriptionResponse>(create.Error);
        }

        var entity = create.Value;

        // 4.1.2 — subscription + activity.created in one unit of work
        await _db.Subscriptions.AddAsync(entity, cancellationToken);
        await _db.ActivityLogs.AddAsync(
            ActivityLog.Create(
                userId: userId,
                entityType: ActivityLogConstants.EntityTypes.Subscription,
                action: ActivityLogConstants.Actions.SubscriptionCreated,
                description: $"Created subscription '{entity.Name}'.",
                entityId: entity.Id,
                newValues: SubscriptionActivitySnapshots.Capture(entity),
                ipAddress: ResolveClientIp(),
                userAgent: ResolveUserAgent()),
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var loaded = await _db.Subscriptions
            .AsNoTracking()
            .IncludeDetails()
            .FirstAsync(s => s.Id == entity.Id, cancellationToken);

        return Result.Success(
            CreateSubscriptionResponse.FromSubscription(SubscriptionResponse.FromEntity(loaded)));
    }

    private string? ResolveClientIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        var forwarded = ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        return ctx?.Connection.RemoteIpAddress?.ToString();
    }

    private string? ResolveUserAgent()
    {
        var ua = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }

}
