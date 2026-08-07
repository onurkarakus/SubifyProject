using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Activity.ListActivity;

/// <summary>
/// List current user's activity logs (5.4.2). Pagination + optional entityType filter.
/// </summary>
public sealed record ListActivityQuery(
    string? EntityType = null,
    int Page = SubscriptionConstants.DefaultPage,
    int PageSize = 10) : IRequest<Result<ListActivityResponse>>;

public sealed class ListActivityValidator : AbstractValidator<ListActivityQuery>
{
    public ListActivityValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, SubscriptionConstants.MaxPageSize);
        RuleFor(x => x.EntityType)
            .MaximumLength(50)
            .When(x => x.EntityType is not null);
    }
}

public sealed class ListActivityHandler : IRequestHandler<ListActivityQuery, Result<ListActivityResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListActivityHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListActivityResponse>> Handle(
        ListActivityQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ListActivityResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, SubscriptionConstants.MaxPageSize);

        var query = _db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var type = request.EntityType.Trim().ToLowerInvariant();
            // Prefer exact stored values; also accept case-insensitive match via lower().
            query = query.Where(a => a.EntityType.ToLower() == type);
        }

        // Materialize then order: SQLite cannot ORDER BY DateTimeOffset (Postgres can).
        // Activity volume per user is small for MVP self-host; optimize later if needed.
        var all = await query.ToListAsync(cancellationToken);
        var totalItems = all.Count;

        var items = all
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityItemResponse(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.Description,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt))
            .ToList();

        return Result.Success(new ListActivityResponse(
            Data: items,
            Pagination: PaginationInfo.Create(page, pageSize, totalItems)));
    }
}
