using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
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
        var oldPrice = entity.Price;
        var oldCurrency = entity.Currency;

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
            notes: request.Notes);

        if (update.IsFailure)
        {
            return Result.Failure<SubscriptionResponse>(update.Error);
        }

        // 16.4.1 — price/currency change audit
        var priceChanged = oldPrice != entity.Price
            || !string.Equals(oldCurrency, entity.Currency, StringComparison.OrdinalIgnoreCase);
        if (priceChanged)
        {
            _db.SubscriptionPriceHistories.Add(SubscriptionPriceHistory.Create(
                subscriptionId: entity.Id,
                userId: userId,
                oldPrice: oldPrice,
                oldCurrency: oldCurrency,
                newPrice: entity.Price,
                newCurrency: entity.Currency));
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

        // Materialize then order (SQLite DateTimeOffset ORDER BY safety)
        var history = await _db.SubscriptionPriceHistories
            .AsNoTracking()
            .Where(h => h.SubscriptionId == entity.Id)
            .ToListAsync(cancellationToken);

        var dtos = history
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Take(20)
            .Select(SubscriptionPriceChangeDto.FromEntity)
            .ToList();
        return Result.Success(SubscriptionResponse.FromEntity(
            loaded,
            latestPriceChange: dtos.FirstOrDefault(),
            priceHistory: dtos));
    }
}
