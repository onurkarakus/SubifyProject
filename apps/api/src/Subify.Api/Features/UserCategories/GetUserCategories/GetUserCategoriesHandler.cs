using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.UserCategories.GetUserCategories;

public class GetUserCategoriesHandler : IRequestHandler<GetUserCategoriesQuery, Result<List<GetUserCategoryResponse>>>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetUserCategoriesHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<GetUserCategoryResponse>>> Handle(GetUserCategoriesQuery request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure<List<GetUserCategoryResponse>>(DomainErrors.User.UnAuthorized);
        }

        var categories = await _context.UserCategories
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.CreatedAt)
            .Select(uc => new GetUserCategoryResponse(
                uc.Id,
                uc.Name,  
                uc.Slug,
                uc.Icon ?? "folder", 
                uc.Color ?? "#808080",
                uc.IsActive,
                uc.SortOrder
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(categories);
    }
}
