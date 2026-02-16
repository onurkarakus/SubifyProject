using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.Categories.GetCategories;

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

        var resourceKeys = categories.Select(c => $"Category_{c.Slug}").ToList();

        var resources = await dbContext.Resources
            .AsNoTracking()
            .Where(r => r.PageName == "Category" && r.LanguageCode == userLocale && resourceKeys.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, r => r.Value, cancellationToken);

        var response = categories.Select(c =>
        {
            var resourceKey = $"Category_{c.Slug}";
            var displayName = resources.TryGetValue(resourceKey, out var translation) ? translation : c.Slug;

            return new GetCategoryResponse(
                c.Id,
                displayName,
                c.Slug,
                c.Icon,
                c.Color,
                c.SortOrder,
                c.IsActive,
                true // IsDefault/System = true
            );
        }).ToList();

        return Result.Success(response);
    }
}
