using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Users.ListAdminUsers;

/// <summary>
/// Paginated admin user list with search (7.1.1). SuperAdmin or Admin.
/// Does not return subscription entities (7.1.4) — only an active count.
/// </summary>
public sealed record ListAdminUsersQuery(
    string? Search = null,
    int Page = SubscriptionConstants.DefaultPage,
    int PageSize = SubscriptionConstants.DefaultPageSize)
    : IRequest<Result<ListAdminUsersResponse>>;

public sealed class ListAdminUsersValidator : AbstractValidator<ListAdminUsersQuery>
{
    public ListAdminUsersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, SubscriptionConstants.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(SubscriptionConstants.SearchMaxLength)
            .When(x => x.Search is not null);
    }
}

public sealed class ListAdminUsersHandler
    : IRequestHandler<ListAdminUsersQuery, Result<ListAdminUsersResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubifyDbContext _db;

    public ListAdminUsersHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        ISubifyDbContext db)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result<ListAdminUsersResponse>> Handle(
        ListAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        var access = AdminUserAccess.RequireAdminOrAbove(_currentUser);
        if (access.IsFailure)
        {
            return Result.Failure<ListAdminUsersResponse>(access.Error);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, SubscriptionConstants.MaxPageSize);

        // Materialize users for SQLite-friendly search/sort (small multi-user OS instance).
        var users = await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            users = users
                .Where(u =>
                    (u.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        users = users
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = users.Count;
        var slice = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var ids = slice.Select(u => u.Id).ToList();
        var counts = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => ids.Contains(s.UserId) && !s.Archived && s.DeletedAt == null)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countMap = counts.ToDictionary(c => c.UserId, c => c.Count);

        var data = new List<AdminUserResponse>(slice.Count);
        foreach (var user in slice)
        {
            countMap.TryGetValue(user.Id, out var subCount);
            data.Add(await AdminUserMapper.ToResponseAsync(
                _userManager, user, subCount, cancellationToken));
        }

        return Result.Success(new ListAdminUsersResponse(
            Data: data,
            Pagination: PaginationInfo.Create(page, pageSize, total)));
    }
}
