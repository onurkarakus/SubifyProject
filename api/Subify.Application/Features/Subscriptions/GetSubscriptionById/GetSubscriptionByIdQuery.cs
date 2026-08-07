using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.GetSubscriptionById;

/// <summary>Get one subscription by id (4.1.3). Ownership enforced.</summary>
public sealed record GetSubscriptionByIdQuery(Guid Id) : IRequest<Result<SubscriptionResponse>>;

public sealed class GetSubscriptionByIdHandler
    : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSubscriptionByIdHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<SubscriptionResponse>> Handle(
        GetSubscriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        // Ignore soft-delete filter so archived (DeletedAt set) rows are still loadable by owner.
        var entity = await _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .IncludeDetails()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<SubscriptionResponse>(DomainErrors.Subscription.SubscriptionNotFound);
        }

        if (entity.UserId != userId)
        {
            // Task 4.1.3: explicit 403 for foreign ownership (not silent 404).
            return Result.Failure<SubscriptionResponse>(DomainErrors.Subscription.SubscriptionAccessDenied);
        }

        // Materialize then order (SQLite DateTimeOffset ORDER BY safety)
        var history = await _db.SubscriptionPriceHistories
            .AsNoTracking()
            .Where(h => h.SubscriptionId == entity.Id && h.UserId == userId)
            .ToListAsync(cancellationToken);

        var dtos = history
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Take(20)
            .Select(SubscriptionPriceChangeDto.FromEntity)
            .ToList();
        return Result.Success(SubscriptionResponse.FromEntity(
            entity,
            latestPriceChange: dtos.FirstOrDefault(),
            priceHistory: dtos));
    }
}
