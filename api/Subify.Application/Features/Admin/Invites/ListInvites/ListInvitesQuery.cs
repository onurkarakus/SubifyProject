using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Admin.Users;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Invites.ListInvites;

/// <summary>
/// Pending invites (7.2.2): not used and not expired. SuperAdmin/Admin.
/// </summary>
public sealed record ListInvitesQuery(bool IncludeExpired = false)
    : IRequest<Result<ListInvitesResponse>>;

public sealed class ListInvitesHandler
    : IRequestHandler<ListInvitesQuery, Result<ListInvitesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListInvitesHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListInvitesResponse>> Handle(
        ListInvitesQuery request,
        CancellationToken cancellationToken)
    {
        var access = AdminUserAccess.RequireAdminOrAbove(_currentUser);
        if (access.IsFailure)
        {
            return Result.Failure<ListInvitesResponse>(access.Error);
        }

        var now = DateTimeOffset.UtcNow;

        // Materialize then filter — SQLite DateTimeOffset comparison safety + IsPending helper.
        var rows = await _db.UserInvites
            .AsNoTracking()
            .Where(i => i.UsedAt == null)
            .ToListAsync(cancellationToken);

        if (!request.IncludeExpired)
        {
            rows = rows.Where(i => !i.IsExpired(now)).ToList();
        }

        var data = rows
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Email, StringComparer.OrdinalIgnoreCase)
            .Select(i => new InviteListItemResponse(
                Id: i.Id,
                Email: i.Email,
                ExpiresAt: i.ExpiresAt,
                CreatedAt: i.CreatedAt,
                CreatedByUserId: i.CreatedByUserId,
                IsPending: i.IsPending(now)))
            .ToList();

        return Result.Success(new ListInvitesResponse(data));
    }
}
