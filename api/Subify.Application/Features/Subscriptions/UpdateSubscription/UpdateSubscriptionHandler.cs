using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.UpdateSubscription;

public sealed class UpdateSubscriptionHandler
    : IRequestHandler<UpdateSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activityLogger;

    public UpdateSubscriptionHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IActivityLogger activityLogger)
    {
        _db = db;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        UpdateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        if (!CreateSubscriptionValidator.TryParseBillingCycle(request.BillingCycle, out var billingCycle))
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.Subscription.InvalidBillingCycle);
        }

        var entity = await _db.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.Subscription.SubscriptionNotFound);
        }

        if (entity.UserId != userId)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.Subscription.SubscriptionAccessDenied);
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
            return Result.Failure<SubscriptionResponse>(refs.Error);
        }

        var oldValues = SubscriptionActivitySnapshots.Capture(entity);

        var update = entity.Update(
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

        if (update.IsFailure)
        {
            return Result.Failure<SubscriptionResponse>(update.Error);
        }

        await _activityLogger.LogAsync(
            userId: userId,
            entityType: ActivityLogConstants.EntityTypes.Subscription,
            action: ActivityLogConstants.Actions.SubscriptionUpdated,
            description: $"Updated subscription '{entity.Name}'.",
            entityId: entity.Id,
            oldValues: oldValues,
            newValues: SubscriptionActivitySnapshots.Capture(entity),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var loaded = await _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .IncludeDetails()
            .FirstAsync(s => s.Id == entity.Id, cancellationToken);

        return Result.Success(SubscriptionResponse.FromEntity(loaded));
    }
}
