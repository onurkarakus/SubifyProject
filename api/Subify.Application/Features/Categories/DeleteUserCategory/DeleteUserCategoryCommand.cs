using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Categories.DeleteUserCategory;

/// <summary>
/// Soft-delete own user category (5.1.5).
/// Conflict if any non-archived subscription still references it.
/// </summary>
public sealed record DeleteUserCategoryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteUserCategoryHandler : IRequestHandler<DeleteUserCategoryCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteUserCategoryHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteUserCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        var entity = await _db.UserCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(DomainErrors.UserCategoryErrors.NotFound);
        }

        if (entity.UserId != userId)
        {
            return Result.Failure(DomainErrors.UserCategoryErrors.AccessDenied);
        }

        var hasActiveSubs = await _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(
                s => s.UserCategoryId == entity.Id
                     && !s.Archived,
                cancellationToken);

        if (hasActiveSubs)
        {
            return Result.Failure(DomainErrors.UserCategoryErrors.HasActiveSubscriptions);
        }

        entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
