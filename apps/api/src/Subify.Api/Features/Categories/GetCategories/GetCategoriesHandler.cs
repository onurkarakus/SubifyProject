using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.Categories.GetCategories;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Result<List<GetCategoriesResponse>>>
{
    private readonly SubifyDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public GetCategoriesHandler(SubifyDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<GetCategoriesResponse>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Locality);

        var categories = await dbContext.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new GetCategoriesResponse
            (
                c.Id,
                "",
                c.Slug,
                c.Icon,
                c.Color,
                c.SortOrder,
                c.IsActive,
                c.IsDefault
            ))
            .ToListAsync(cancellationToken);

        return Result<List<GetCategoriesResponse>>.Success(categories);
    }
}
