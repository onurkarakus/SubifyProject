using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Api.Helpers;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.Categories.GetCategory;

public class GetCategoryHandler : IRequestHandler<GetCategoryQuery, Result<List<GetCategoryResponse>>>
{
    private readonly SubifyDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public GetCategoryHandler(SubifyDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<GetCategoryResponse>>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var userLocale = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Locality) ?? "tr-TR";

        var categories = await dbContext.Categories
             .AsNoTracking()
             .Where(c => c.IsActive)
             .OrderBy(c => c.SortOrder)
             .ToListAsync(cancellationToken);

        var response = categories.Select(c =>
        {
            

            return new GetCategoryResponse(
                c.Id,
                c.Slug,
                c.Icon,
                c.Color,
                c.SortOrder,
                c.IsActive,
                true 
            );
        }).ToList();

        return Result.Success(response);
    }
}
