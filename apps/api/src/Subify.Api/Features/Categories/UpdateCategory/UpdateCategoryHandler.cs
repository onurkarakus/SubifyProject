using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Categories.UpdateCategories;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateCategoryHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(DomainErrors.CategoryErrors.NotFound);
        }

        category.Icon = request.Icon;
        category.Color = request.Color;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {            
            var resourceKey = $"Category_{category.Slug}";

            var resourceTr = await _context.Resources
                .FirstOrDefaultAsync(r => r.PageName == "Category" && r.Name == resourceKey && r.LanguageCode == "tr-TR", cancellationToken);

            if (resourceTr != null)
            {
                resourceTr.Value = request.Name;
                resourceTr.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {                
                _context.Resources.Add(new Resource
                {
                    Id = Guid.NewGuid(),
                    Name = resourceKey,
                    LanguageCode = "tr-TR",
                    Value = request.Name,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            var resourceEn = await _context.Resources
                .FirstOrDefaultAsync(r => r.PageName == "Category" && r.Name == resourceKey && r.LanguageCode == "en-US", cancellationToken);

            if (resourceEn != null)
            {
                resourceEn.Value = request.Name;
                resourceEn.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                _context.Resources.Add(new Resource
                {
                    Id = Guid.NewGuid(),
                    Name = resourceKey,
                    LanguageCode = "en-US",
                    Value = request.Name,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}