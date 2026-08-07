using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Categories.GetUserCategories;

/// <summary>List current user's custom categories (5.1.2). Soft-deleted excluded.</summary>
public sealed record GetUserCategoriesQuery : IRequest<Result<ListUserCategoriesResponse>>;

public sealed class GetUserCategoriesHandler
    : IRequestHandler<GetUserCategoriesQuery, Result<ListUserCategoriesResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserCategoriesHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListUserCategoriesResponse>> Handle(
        GetUserCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ListUserCategoriesResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;

        var items = await _db.UserCategories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new UserCategoryResponse(
                c.Id,
                c.Name,
                c.Icon,
                c.Color,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new ListUserCategoriesResponse(items));
    }
}
