using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.ReactivateSubscription;

/// <summary>Restore an archived subscription (4.1.8).</summary>
public sealed record ReactivateSubscriptionCommand(Guid Id) : IRequest<Result<SubscriptionResponse>>;

public sealed class ReactivateSubscriptionHandler
    : IRequestHandler<ReactivateSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activityLogger;

    public ReactivateSubscriptionHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IActivityLogger activityLogger)
    {
        _db = db;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        ReactivateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

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

        if (entity.Archived || entity.DeletedAt is not null)
        {
            var oldValues = SubscriptionActivitySnapshots.Capture(entity);
            entity.Reactivate();

            await _activityLogger.LogAsync(
                userId: userId,
                entityType: ActivityLogConstants.EntityTypes.Subscription,
                action: ActivityLogConstants.Actions.SubscriptionReactivated,
                description: $"Reactivated subscription '{entity.Name}'.",
                entityId: entity.Id,
                oldValues: oldValues,
                newValues: SubscriptionActivitySnapshots.Capture(entity),
                cancellationToken: cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        var loaded = await _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .IncludeDetails()
            .FirstAsync(s => s.Id == entity.Id, cancellationToken);

        return Result.Success(SubscriptionResponse.FromEntity(loaded));
    }
}
